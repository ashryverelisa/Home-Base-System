const formats = ["ean_13", "ean_8", "upc_a"];
const ponyfillUrl = "https://cdn.jsdelivr.net/npm/barcode-detector@3.2.2/dist/es/ponyfill.min.js";
const scanIntervalMs = 200;

let generation = 0;
let session = null;

async function createDetector() {
    if ("BarcodeDetector" in window) {
        const supported = await window.BarcodeDetector.getSupportedFormats();
        const usable = formats.filter(f => supported.includes(f));

        if (usable.length > 0) {
            return new window.BarcodeDetector({ formats: usable });
        }
    }

    // Fallback for browsers without native support (Firefox, Safari): ZXing compiled to WASM.
    const { BarcodeDetector } = await import(ponyfillUrl);
    return new BarcodeDetector({ formats });
}

export async function start(video, dotnet) {
    stop();
    const mine = generation;

    if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia) {
        return "insecure";
    }

    let stream;

    try {
        stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: { ideal: "environment" } },
            audio: false
        });
    } catch (error) {
        return error?.name === "NotAllowedError" ? "denied" : "unavailable";
    }

    if (mine !== generation) {
        stream.getTracks().forEach(track => track.stop());
        return "stopped";
    }

    session = { stream, timer: 0 };

    try {
        video.srcObject = stream;
        await video.play();

        const detector = await createDetector();

        const tick = async () => {
            if (mine !== generation) {
                return;
            }

            try {
                const codes = await detector.detect(video);

                if (codes.length > 0 && mine === generation
                    && await dotnet.invokeMethodAsync("OnDetected", codes[0].rawValue)) {
                    stop();
                    return;
                }
            } catch {
                // Frame not ready yet; try again on the next tick.
            }

            if (mine === generation) {
                session.timer = setTimeout(tick, scanIntervalMs);
            }
        };

        tick();
        return mine === generation ? "running" : "stopped";
    } catch {
        if (mine === generation) {
            stop();
        }

        return "unavailable";
    }
}

export function stop() {
    generation++;

    if (session === null) {
        return;
    }

    clearTimeout(session.timer);
    session.stream.getTracks().forEach(track => track.stop());
    session = null;
}
