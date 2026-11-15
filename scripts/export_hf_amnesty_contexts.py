import argparse
from pathlib import Path

from datasets import load_dataset


def main() -> None:
    parser = argparse.ArgumentParser(description="Export source passages from the Amnesty QA dataset for local RAG indexing.")
    parser.add_argument("--output", type=Path, default=Path("samples/amnesty_qa_contexts.txt"))
    parser.add_argument("--limit", type=int, default=20, help="Maximum evaluation rows to export (1-20).")
    args = parser.parse_args()

    if not 1 <= args.limit <= 20:
        parser.error("--limit must be between 1 and 20.")

    dataset = load_dataset("explodinggradients/amnesty_qa", "english_v3", split="eval")
    passages: list[str] = []
    seen: set[str] = set()
    for row in dataset.select(range(min(args.limit, len(dataset)))):
        contexts = row.get("retrieved_contexts", [])
        if not isinstance(contexts, list):
            continue
        for context in contexts:
            if isinstance(context, str) and context.strip() and context.strip() not in seen:
                seen.add(context.strip())
                passages.append(context.strip())

    if not passages:
        raise RuntimeError("The selected dataset rows contained no retrieved_contexts text.")

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n\n---\n\n".join(passages) + "\n", encoding="utf-8")
    print(f"Exported {len(passages)} passages to {args.output.resolve()}")


if __name__ == "__main__":
    main()
