"""Summarize observed isolated experiments; never edits league/player data."""
import argparse
import csv
import json
from pathlib import Path


def summarize(root: Path):
    reports = [json.loads(p.read_text(encoding="utf-8-sig")) for p in sorted(root.glob("*/report.json"))]
    rows = []
    for report in reports:
        for sample in report["checkpoints"]:
            rating = sample["rating"]
            rows.append(dict(scenario=report["scenario"], policy=report["resetPolicy"], seed=report["seed"],
                population=report["population"], seasons=report["seasons"], quarter=sample["quarter"],
                races=sample["completedRaces"], supply=sample["totalSupply"], reset_removed=sample["resetRemoved"],
                mean=rating["mean"], median=rating["median"], sd=rating["sd"], p10=rating["p10"],
                p90=rating["p90"], p95=rating["p95"], p99=rating["p99"], max=rating["max"],
                after_3000_if_approved=sample["qualifiedUnifiedAfter"], current_hill_beyond=sample["qualifiedHillBeyond"],
                skill_rating_spearman=sample["skillRatingSpearman"], volume_rating_spearman=sample["volumeRatingSpearman"],
                mean_absolute_delta_per_rated_start=sample["meanAbsoluteMmrDeltaPerRatedStart"],
                beginner_rating=sample["tracked"][0]["preResetMmr"], former_elite_rating=sample["tracked"][1]["preResetMmr"],
                inactive_races=sample["tracked"][2]["races"]))
    with (root / "season-measurements.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)
    summary = dict(experiments=len(reports), total_settled_races=sum(r["totalRaces"] for r in reports),
        failures=[dict(seed=r["seed"], scenario=r["scenario"], policy=r["resetPolicy"], failures=r["failures"])
                  for r in reports if r["failures"] or not r["budgetReconciled"] or not r["reloadIdentical"]],
        activated=False, all_final_rows=[r for r in rows if r["quarter"] == r["seasons"]],
        caveats=["Statistical outcome model, not Unity physics or mobile evidence.",
                 "1200-identity samples do not establish a 100k equilibrium.",
                 "Mobility changes hidden capability in a marked fixture; it never scripts race winners.",
                 "The two threshold curves retain Hill's existing post-curve 10000 ceiling.",
                 "Different matchmaking after rating changes means results are comparable seeded scenarios, not identical race trajectories."])
    (root / "summary.json").write_text(json.dumps(summary, indent=2, allow_nan=False), encoding="utf-8")
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    plt.rcParams.update({"font.size": 10, "axes.spines.top": False, "axes.spines.right": False})
    fig, axes = plt.subplots(1, 3, figsize=(17, 5), constrained_layout=True)
    colors = {"existing": "#157eb1", "threshold-2500-85": "#bc6b16", "threshold-1000-95": "#24875d"}
    labels = {"existing": "Hill actual", "threshold-2500-85": "Umbral 2500 / 85%", "threshold-1000-95": "Umbral 1000 / 95%"}
    for report in reports:
        policy = report["resetPolicy"]
        if report["scenario"] == "organic" and report["seasons"] == 24:
            x = [c["quarter"] for c in report["checkpoints"]]
            y = [c["rating"]["mean"] for c in report["checkpoints"]]
            axes[0].plot(x, y, color=colors[policy], alpha=.8, linestyle="-" if report["seed"] == 4207 else "--",
                         label=labels[policy] if report["seed"] == 4207 else None)
            y = [c["qualifiedUnifiedAfter"] / report["population"] * 100 for c in report["checkpoints"]]
            axes[1].plot(x, y, color=colors[policy], alpha=.8, linestyle="-" if report["seed"] == 4207 else "--")
        if report["scenario"] == "mobility" and policy == "existing":
            x = [c["quarter"] for c in report["checkpoints"]]
            for node, label, color in [(0, "Principiante mejora habilidad", "#24875d"), (1, "Elite pierde habilidad", "#c94747")]:
                axes[2].plot(x, [c["tracked"][node]["preResetMmr"] for c in report["checkpoints"]], label=label, color=color)
            axes[2].axvline(2, color="#888888", linestyle=":", label="Cambio de habilidad (fixture)")
    axes[0].set(title="Economia: media antes del reset", xlabel="Temporada trimestral", ylabel="MMR medio")
    axes[0].legend(fontsize=8)
    axes[1].set(title="AFTER >= 3000 si se aprobara", xlabel="Temporada trimestral", ylabel="Poblacion clasificada (%)")
    axes[2].set(title="Movilidad por resultados del simulador", xlabel="Temporada trimestral", ylabel="MMR antes del reset")
    axes[2].legend(fontsize=8)
    for ax in axes:
        ax.grid(alpha=.2)
    fig.suptitle("Hill Adventurer: experimentos aislados, sin activacion ni migracion de perfiles", fontsize=14)
    fig.savefig(root / "economy-comparison.png", dpi=160)
    fig.savefig(root / "economy-comparison.svg")
    plt.close(fig)
    print(json.dumps(dict(experiments=summary["experiments"], settledRaces=summary["total_settled_races"], failures=summary["failures"])))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    summarize(parser.parse_args().root)
