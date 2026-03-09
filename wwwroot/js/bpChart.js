(() => {
    const COLORS = {
        systolic: "#d62828",
        diastolic: "#2563eb",
        pulse: "#475569",
        axis: "#94a3b8",
        grid: "#e2e8f0",
        label: "#334155"
    };

    function getCanvasContext() {
        const canvas = document.getElementById("bpChart");
        if (!canvas) {
            return null;
        }

        const width = canvas.clientWidth || canvas.width || 600;
        const height = canvas.clientHeight || canvas.height || 260;
        const dpr = window.devicePixelRatio || 1;

        canvas.width = Math.floor(width * dpr);
        canvas.height = Math.floor(height * dpr);

        const ctx = canvas.getContext("2d");
        if (!ctx) {
            return null;
        }

        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        return { ctx, width, height };
    }

    function drawEmptyState(ctx, width, height) {
        ctx.clearRect(0, 0, width, height);
        ctx.fillStyle = COLORS.label;
        ctx.font = "14px Segoe UI, sans-serif";
        ctx.textAlign = "center";
        ctx.fillText("Noch keine Messwerte vorhanden.", width / 2, height / 2);
    }

    function drawGrid(ctx, area, minY, maxY) {
        const gridLines = 5;

        ctx.strokeStyle = COLORS.grid;
        ctx.lineWidth = 1;
        ctx.setLineDash([]);

        for (let i = 0; i <= gridLines; i += 1) {
            const y = area.top + (i / gridLines) * area.height;
            ctx.beginPath();
            ctx.moveTo(area.left, y);
            ctx.lineTo(area.left + area.width, y);
            ctx.stroke();

            const value = Math.round(maxY - (i / gridLines) * (maxY - minY));
            ctx.fillStyle = COLORS.axis;
            ctx.font = "11px Segoe UI, sans-serif";
            ctx.textAlign = "right";
            ctx.fillText(value.toString(), area.left - 8, y + 4);
        }

        ctx.strokeStyle = COLORS.axis;
        ctx.beginPath();
        ctx.moveTo(area.left, area.top + area.height);
        ctx.lineTo(area.left + area.width, area.top + area.height);
        ctx.stroke();
    }

    function drawSeries(ctx, points, area, minY, maxY, color, options = {}) {
        const yRange = Math.max(1, maxY - minY);
        const stepX = points.length > 1 ? area.width / (points.length - 1) : 0;

        const toX = (index) => area.left + index * stepX;
        const toY = (value) => area.top + ((maxY - value) / yRange) * area.height;

        ctx.strokeStyle = color;
        ctx.lineWidth = options.lineWidth ?? 2;
        ctx.setLineDash(options.dash ?? []);

        ctx.beginPath();
        points.forEach((point, index) => {
            const x = toX(index);
            const y = toY(point.value);
            if (index === 0) {
                ctx.moveTo(x, y);
            } else {
                ctx.lineTo(x, y);
            }
        });
        ctx.stroke();

        points.forEach((point, index) => {
            const x = toX(index);
            const y = toY(point.value);

            ctx.beginPath();
            ctx.setLineDash([]);
            ctx.fillStyle = color;
            ctx.arc(x, y, options.pointRadius ?? 3, 0, Math.PI * 2);
            ctx.fill();
        });
    }

    function drawXAxisLabels(ctx, labels, area) {
        if (!labels.length) {
            return;
        }

        const maxLabels = 6;
        const stride = Math.max(1, Math.ceil(labels.length / maxLabels));
        const stepX = labels.length > 1 ? area.width / (labels.length - 1) : 0;

        ctx.fillStyle = COLORS.axis;
        ctx.font = "11px Segoe UI, sans-serif";
        ctx.textAlign = "center";

        labels.forEach((label, index) => {
            if (index % stride !== 0 && index !== labels.length - 1) {
                return;
            }

            const x = area.left + index * stepX;
            ctx.fillText(label, x, area.top + area.height + 18);
        });
    }

    window.bpChart = {
        render: (rawPoints) => {
            const canvasInfo = getCanvasContext();
            if (!canvasInfo) {
                return;
            }

            const { ctx, width, height } = canvasInfo;
            const points = (rawPoints || [])
                .filter((x) => Number.isFinite(x.systolic) && Number.isFinite(x.diastolic) && Number.isFinite(x.pulse))
                .map((x) => ({
                    label: x.label,
                    systolic: Number(x.systolic),
                    diastolic: Number(x.diastolic),
                    pulse: Number(x.pulse)
                }));

            if (points.length === 0) {
                drawEmptyState(ctx, width, height);
                return;
            }

            const values = points.flatMap((x) => [x.systolic, x.diastolic, x.pulse]);
            const minY = Math.floor(Math.min(...values) - 10);
            const maxY = Math.ceil(Math.max(...values) + 10);

            const area = {
                left: 50,
                top: 12,
                width: Math.max(100, width - 65),
                height: Math.max(120, height - 48)
            };

            ctx.clearRect(0, 0, width, height);
            drawGrid(ctx, area, minY, maxY);

            drawSeries(
                ctx,
                points.map((x) => ({ value: x.systolic })),
                area,
                minY,
                maxY,
                COLORS.systolic,
                { pointRadius: 3, lineWidth: 2 }
            );

            drawSeries(
                ctx,
                points.map((x) => ({ value: x.diastolic })),
                area,
                minY,
                maxY,
                COLORS.diastolic,
                { pointRadius: 3, lineWidth: 2 }
            );

            drawSeries(
                ctx,
                points.map((x) => ({ value: x.pulse })),
                area,
                minY,
                maxY,
                COLORS.pulse,
                { pointRadius: 2.5, lineWidth: 2, dash: [2, 6] }
            );

            drawXAxisLabels(ctx, points.map((x) => x.label), area);
        }
    };
})();
