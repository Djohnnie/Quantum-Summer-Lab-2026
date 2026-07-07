#!/usr/bin/env python3
"""Regenerate QuantumSummerLab.Tools/Challenges.cs from the _challenges/*/src/Main.qs files.

Each Main.qs is the single source of truth for one challenge:
  - A "CHALLENGE METADATA" comment block at the top holds the plain-text
    fields (Name, Title, Description, Tldr, ExampleDescription, ExampleCode,
    ExpectedOutput, CopilotInstructions, Level), plus ExpectedStates as a
    readable JSON block delimited by `// ===EXPECTED-STATES-START===` /
    `// ===EXPECTED-STATES-END===`.
  - The real Q# code below it is a runnable file. The reference solution is
    marked with `// ===SOLVE-START===` / `// ===SOLVE-END===` comments.

From that, this script derives the fields that used to be hand-copied as
opaque Base64 and went stale:
  - SolutionTemplate: the Solve operation's signature with a stub body.
  - Solution: the full reference Solve implementation (the SOLVE block).
  - VerificationTemplate: the whole file with the Solve implementation cut
    out and replaced by the `<<SOLVE>>` placeholder.
  - ExpectedStates: the JSON block, re-encoded to Base64.

Usage:
    python _challenges/generate_challenges.py           # write Challenges.cs
    python _challenges/generate_challenges.py --check   # exit 1 if stale, don't write
"""
import base64
import re
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parent
OUTPUT_PATH = REPO_ROOT / "QuantumSummerLab.Tools" / "Challenges.cs"

SEPARATOR = "// " + "=" * 60

# Fields stored as flat `// Field = "value"` comment lines.
SIMPLE_FIELDS = [
    "Name", "Title", "Description", "Tldr",
    "ExampleDescription", "ExampleCode",
    "ExpectedOutput", "CopilotInstructions", "Level",
]

# All fields the metadata block must ultimately provide (SIMPLE_FIELDS plus
# ExpectedStates, which is stored as a readable JSON block instead).
METADATA_FIELDS = SIMPLE_FIELDS + ["ExpectedStates"]

FIELD_LINE_RE = re.compile(
    r'^//\s*(' + "|".join(SIMPLE_FIELDS) + r')\s*=\s*(.*)$'
)

SOLVE_START_MARKER = "// ===SOLVE-START==="
SOLVE_END_MARKER = "// ===SOLVE-END==="

EXPECTED_STATES_START_MARKER = "// ===EXPECTED-STATES-START==="
EXPECTED_STATES_END_MARKER = "// ===EXPECTED-STATES-END==="

STUB_BODY = "    // Your solution logic goes here."


class ChallengeParseError(Exception):
    pass


def extract_expected_states_json(metadata_section, path):
    """Pull the readable JSON block back out from between the EXPECTED-STATES markers."""
    try:
        start_idx = metadata_section.index(EXPECTED_STATES_START_MARKER)
        end_idx = metadata_section.index(EXPECTED_STATES_END_MARKER)
    except ValueError:
        raise ChallengeParseError(
            f"{path}: could not find {EXPECTED_STATES_START_MARKER} / {EXPECTED_STATES_END_MARKER} markers"
        )

    inner = metadata_section[start_idx + len(EXPECTED_STATES_START_MARKER):end_idx].strip("\n")
    if not inner:
        return ""

    json_lines = []
    for line in inner.split("\n"):
        stripped = line.strip()
        if stripped == "//":
            json_lines.append("")
        elif stripped.startswith("// "):
            json_lines.append(stripped[3:])
        else:
            raise ChallengeParseError(f"{path}: unexpected line in ExpectedStates block: {line!r}")
    return "\n".join(json_lines)


def parse_metadata(text, path):
    """Extract the flat `// Field = value` lines from the metadata header block."""
    separator_positions = [m.start() for m in re.finditer(re.escape(SEPARATOR), text)]
    if len(separator_positions) < 3:
        raise ChallengeParseError(
            f"{path}: expected 3 metadata separator lines, found {len(separator_positions)}"
        )
    metadata_end = text.index("\n", separator_positions[2]) + 1
    metadata_section = text[:metadata_end]
    code_section = text[metadata_end:].lstrip("\n")

    fields = {}
    for line in metadata_section.splitlines():
        m = FIELD_LINE_RE.match(line.strip())
        if not m:
            continue
        key, raw_value = m.group(1), m.group(2).strip()
        if key == "Level":
            fields[key] = raw_value
        else:
            if not (raw_value.startswith('"') and raw_value.endswith('"')):
                raise ChallengeParseError(f"{path}: field {key!r} is not a quoted string: {raw_value!r}")
            fields[key] = raw_value[1:-1]

    expected_states_json = extract_expected_states_json(metadata_section, path)
    fields["ExpectedStates"] = b64(expected_states_json)

    missing = [f for f in METADATA_FIELDS if f not in fields]
    if missing:
        raise ChallengeParseError(f"{path}: missing metadata fields: {missing}")

    return fields, code_section


def extract_solve(code_section, path):
    """Split code_section into (before, solve_body, after) around the SOLVE markers."""
    try:
        start_idx = code_section.index(SOLVE_START_MARKER)
        end_idx = code_section.index(SOLVE_END_MARKER)
    except ValueError:
        raise ChallengeParseError(f"{path}: could not find {SOLVE_START_MARKER} / {SOLVE_END_MARKER} markers")

    before = code_section[:start_idx].rstrip("\n")
    solve_body = code_section[start_idx + len(SOLVE_START_MARKER):end_idx].strip("\n")
    after = code_section[end_idx + len(SOLVE_END_MARKER):].lstrip("\n").rstrip("\n")

    return before, solve_body, after


def build_solution_template(solve_body, path):
    """Signature of the Solve operation, with a stub body."""
    brace_idx = solve_body.find("{")
    if brace_idx == -1:
        raise ChallengeParseError(f"{path}: Solve operation has no opening brace")
    signature = solve_body[:brace_idx].rstrip("\n")
    return f"{signature}\n{{\n{STUB_BODY}\n}}"


def build_verification_template(before, after):
    """Everything except the Solve implementation, with <<SOLVE>> in its place."""
    parts = [before.rstrip("\n"), "", "<<SOLVE>>"]
    if after.strip():
        parts.append("")
        parts.append(after)
    return "\n".join(parts)


def b64(text):
    return base64.b64encode(text.encode("utf-8")).decode("ascii")


def render_challenge_block(fields, solution_template_b64, solution_b64, verification_b64):
    name = fields["Name"]
    lines = [
        f'    public static Challenge CHALLENGE_{name} = new Challenge',
        '    {',
        f'        Name = "{fields["Name"]}",',
        f'        Title = "{fields["Title"]}",',
        f'        Description = "{fields["Description"]}",',
        f'        Tldr = "{fields["Tldr"]}",',
        f'        SolutionTemplate = "{solution_template_b64}",',
        f'        Solution = "{solution_b64}",',
        f'        ExampleDescription = "{fields["ExampleDescription"]}",',
        f'        ExampleCode = "{fields["ExampleCode"]}",',
        f'        VerificationTemplate = "{verification_b64}",',
        f'        ExpectedOutput = "{fields["ExpectedOutput"]}",',
        f'        ExpectedStates = "{fields["ExpectedStates"]}",',
        f'        CopilotInstructions = "{fields["CopilotInstructions"]}",',
        f'        Level = {fields["Level"]}',
        '    };',
    ]
    return "\n".join(lines)


def natural_key(name):
    m = re.match(r'^([A-Za-z]*)(\d*)$', name)
    return (m.group(1), int(m.group(2)) if m.group(2) else -1)


def group_prefix(name):
    """Group key for the All list: the leading letters of the name (e.g. 'D1' -> 'D', '0' -> '')."""
    return natural_key(name)[0]


def render_all_list(names_by_group):
    lines = ['    public static readonly IReadOnlyList<Challenge> All = new[]', '    {']
    for group in sorted(names_by_group):
        names = names_by_group[group]
        entries = ", ".join(f"CHALLENGE_{n}" for n in names)
        lines.append(f'        {entries},')
    lines.append('    };')
    return "\n".join(lines)


def main():
    check_only = "--check" in sys.argv

    main_files = sorted(SCRIPT_DIR.glob("*/src/Main.qs"))
    if not main_files:
        print(f"No */src/Main.qs files found under {SCRIPT_DIR}", file=sys.stderr)
        return 1

    blocks = []
    names_by_group = {}

    for path in main_files:
        text = path.read_text(encoding="utf-8")
        try:
            fields, code_section = parse_metadata(text, path)
            before, solve_body, after = extract_solve(code_section, path)
            solution_template_src = build_solution_template(solve_body, path)
            verification_src = build_verification_template(before, after)
        except ChallengeParseError as e:
            print(f"ERROR: {e}", file=sys.stderr)
            return 1

        block = render_challenge_block(fields, b64(solution_template_src), b64(solve_body), b64(verification_src))
        blocks.append((fields["Name"], block))

        names_by_group.setdefault(group_prefix(fields["Name"]), []).append(fields["Name"])

    for group in names_by_group:
        names_by_group[group].sort(key=natural_key)

    blocks.sort(key=lambda nb: natural_key(nb[0]))

    output_lines = [
        "// <auto-generated>",
        "// This file is generated by _challenges/generate_challenges.py from the",
        "// _challenges/*/src/Main.qs files. Do not edit by hand - edit the source",
        "// Main.qs files and re-run the generator instead.",
        "// </auto-generated>",
        "using QuantumSummerLab.Data.Model;",
        "",
        "namespace QuantumSummerLab.Tools;",
        "",
        "public static class Challenges",
        "{",
    ]
    output_lines.append("\n\n".join(block for _, block in blocks))
    output_lines.append("")
    output_lines.append(render_all_list(names_by_group))
    output_lines.append("}")
    output_lines.append("")

    generated = "\n".join(output_lines)

    if check_only:
        current = OUTPUT_PATH.read_text(encoding="utf-8-sig") if OUTPUT_PATH.exists() else None
        if current == generated:
            print("Challenges.cs is up to date.")
            return 0
        print("Challenges.cs is STALE relative to _challenges/*/src/Main.qs.", file=sys.stderr)
        return 1

    OUTPUT_PATH.write_text(generated, encoding="utf-8-sig", newline="\n")
    print(f"Wrote {len(blocks)} challenges to {OUTPUT_PATH}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
