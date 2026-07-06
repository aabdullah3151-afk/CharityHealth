window.charityQrScanner = window.charityQrScanner || {
    stream: null,
    detector: null,
    timer: null,

    start: async function (videoId, dotNetRef) {
        this.stop();

        const video = document.getElementById(videoId);
        if (!video) {
            throw new Error("QR video element not found.");
        }

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            throw new Error("Camera API is not supported.");
        }

        if (!("BarcodeDetector" in window)) {
            throw new Error("BarcodeDetector API is not supported.");
        }

        const formats = await window.BarcodeDetector.getSupportedFormats();
        if (!formats.includes("qr_code")) {
            throw new Error("QR barcode format is not supported.");
        }

        this.detector = new window.BarcodeDetector({ formats: ["qr_code"] });

        this.stream = await navigator.mediaDevices.getUserMedia({
            video: {
                facingMode: { ideal: "environment" },
                width: { ideal: 1280 },
                height: { ideal: 720 }
            },
            audio: false
        });

        video.srcObject = this.stream;
        await video.play();

        this.timer = window.setInterval(async () => {
            try {
                if (!video.videoWidth || !video.videoHeight) {
                    return;
                }

                const codes = await this.detector.detect(video);

                if (codes && codes.length > 0) {
                    const raw = codes[0].rawValue || "";
                    if (raw) {
                        await dotNetRef.invokeMethodAsync("OnQrScanned", raw);
                        this.stop();
                    }
                }
            } catch (error) {
                console.warn("QR scan tick failed", error);
            }
        }, 650);
    },

    stop: function () {
        if (this.timer) {
            window.clearInterval(this.timer);
            this.timer = null;
        }

        if (this.stream) {
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
        }
    }
};