// Seeds the Firebase Realtime Database /sessions path with randomized
// session records matching AnalyticsManager.cs's schema, for testing
// reports/dashboards. Run with: node seed_firebase_sessions.js
//
// Usage:
//   node seed_firebase_sessions.js <firebaseDatabaseURL> [count]
//
// Example:
//   node seed_firebase_sessions.js https://ardataset-586e9-default-rtdb.asia-southeast1.firebasedatabase.app/ 50

const https = require("https");
const { URL } = require("url");

const dbUrl = process.argv[2];
const count = parseInt(process.argv[3] || "20", 10);

if (!dbUrl) {
    console.error("Usage: node seed_firebase_sessions.js <firebaseDatabaseURL> [count]");
    process.exit(1);
}

const LANGUAGES = ["English", "Sinhala"];

// 6 scenarios per lesson: full (listened to all feedback) / skip (skipped feedback)
// crossed with 0 / 1 / 2 wrong answers. Values are session_duration in seconds,
// taken directly from reference timing data.
const SCENARIOS = ["full-0err", "full-1err", "full-2err", "skip-0err", "skip-1err", "skip-2err"];

const LESSON_DURATIONS = {
    "Time Lesson": [71, 73, 76, 33, 35, 39],
    "Plant Life Cycle": [100, 104, 113, 30, 34, 39.2],
    "Odd Even Lesson": [83.3, 89.1, 92.7, 37.3, 40.4, 44.8],
    "Circuit Lesson": [80.8, 88.8, 94.8, 30, 37, 44],
};

const LESSONS = Object.keys(LESSON_DURATIONS);

// Every combo of lesson x scenario, so we can pick uniformly at random.
const LESSON_SCENARIO_COMBOS = LESSONS.flatMap((lessonName) =>
    SCENARIOS.map((scenario, i) => ({
        lessonName,
        scenario,
        sessionDuration: LESSON_DURATIONS[lessonName][i],
    }))
);

function randomHex(len) {
    let s = "";
    for (let i = 0; i < len; i++) s += Math.floor(Math.random() * 16).toString(16);
    return s;
}

function randomInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function randomFloat(min, max, decimals = 1) {
    return parseFloat((Math.random() * (max - min) + min).toFixed(decimals));
}

function randomTimestamp(daysBack) {
    const now = Date.now();
    const past = now - randomInt(0, daysBack) * 86400000 - randomInt(0, 86400000);
    const d = new Date(past);
    const pad = (n) => String(n).padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

function generateSession() {
    const combo = LESSON_SCENARIO_COMBOS[randomInt(0, LESSON_SCENARIO_COMBOS.length - 1)];
    const { lessonName, scenario, sessionDuration } = combo;

    const maxQuestions = lessonName === "Time Lesson" ? 4 : 2;
    const errors = parseInt(scenario.match(/(\d)err/)[1], 10);
    const skippedFeedback = scenario.startsWith("skip");
    const errorRate = maxQuestions > 0 ? parseFloat((errors / maxQuestions).toFixed(2)) : 0;

    // Split session_duration into ar_time/quiz_time so interaction_time stays
    // under the total (leftover time = feedback/narration, more of it when
    // feedback isn't skipped).
    const nonInteractiveShare = skippedFeedback ? randomFloat(0.05, 0.15) : randomFloat(0.2, 0.35);
    const interactionTime = parseFloat((sessionDuration * (1 - nonInteractiveShare)).toFixed(1));
    const arShare = randomFloat(0.4, 0.6);
    const arTime = parseFloat((interactionTime * arShare).toFixed(1));
    const quizTime = parseFloat((interactionTime - arTime).toFixed(1));

    return {
        timestamp: randomTimestamp(60),
        device_id: randomHex(32),
        language: LANGUAGES[randomInt(0, 1)],
        lesson_name: lessonName,
        ar_time: arTime,
        quiz_time: quizTime,
        interaction_time: interactionTime,
        quiz_errors: errors,
        quiz_total_questions: maxQuestions,
        quiz_error_rate: errorRate,
        session_duration: sessionDuration,
        retry_count: randomInt(0, 3),
    };
}

function postSession(session) {
    return new Promise((resolve, reject) => {
        const target = new URL(dbUrl.replace(/\/$/, "") + "/sessions.json");
        const body = JSON.stringify(session);

        const req = https.request(
            {
                hostname: target.hostname,
                path: target.pathname + target.search,
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Content-Length": Buffer.byteLength(body),
                },
            },
            (res) => {
                let data = "";
                res.on("data", (chunk) => (data += chunk));
                res.on("end", () => {
                    if (res.statusCode >= 200 && res.statusCode < 300) {
                        resolve(data);
                    } else {
                        reject(new Error(`HTTP ${res.statusCode}: ${data}`));
                    }
                });
            }
        );

        req.on("error", reject);
        req.write(body);
        req.end();
    });
}

async function main() {
    console.log(`Seeding ${count} random session(s) to ${dbUrl}`);
    let ok = 0;
    let failed = 0;

    for (let i = 0; i < count; i++) {
        const session = generateSession();
        try {
            await postSession(session);
            ok++;
            console.log(`[${i + 1}/${count}] OK`, session);
        } catch (err) {
            failed++;
            console.error(`[${i + 1}/${count}] FAILED:`, err.message);
        }
    }

    console.log(`\nDone. ${ok} succeeded, ${failed} failed.`);
}

main();
