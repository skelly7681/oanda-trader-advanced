import re
from pathlib import Path

source = Path("repo.md").read_text(encoding="utf-8")

# Matches headings like:
# ### `backend/src/OandaTrader.Api/Program.cs`
# followed by a fenced code block
pattern = re.compile(
    r"^### `(?P<path>[^`]+)`\s*\n\n```[^\n]*\n(?P<content>.*?)\n```",
    re.MULTILINE | re.DOTALL,
)

count = 0

for match in pattern.finditer(source):
    rel_path = match.group("path").strip()
    content = match.group("content")

    # Skip obvious non-file helper sections if needed
    if rel_path.endswith(".sln") and "Create with:" in source[match.start():match.end() + 200]:
        pass

    file_path = Path(rel_path)
    file_path.parent.mkdir(parents=True, exist_ok=True)
    file_path.write_text(content, encoding="utf-8")
    count += 1
    print(f"Wrote {file_path}")

print(f"\nDone. Wrote {count} files.")