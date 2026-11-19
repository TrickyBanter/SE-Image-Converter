"""Regenerate the resource calculator block catalog from a Space Engineers install.

Reads every cube block definition in Content/Data/CubeBlocks/*.sbc, resolves display
names through the English localization (MyTexts.resx) and writes
src/ImageConversion.Core/Data/SpaceEngineersVanillaBlocks.json.

Usage:
    python tools/GenerateBlockCatalog.py "G:/SteamLibrary/steamapps/common/SpaceEngineers"
"""

from __future__ import annotations

import argparse
import collections
import glob
import json
import os
import sys
import xml.etree.ElementTree as ET

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT_OUTPUT = os.path.join(REPO_ROOT, "src", "ImageConversion.Core", "Data", "SpaceEngineersVanillaBlocks.json")

# Component names the app has always used where they differ from the game's abbreviated labels.
COMPONENT_NAME_OVERRIDES = {
    "Construction": "Construction Component",
    "Detector": "Detector Components",
    "GravityGenerator": "Gravity Generator Components",
    "Medical": "Medical Components",
    "RadioCommunication": "Radio Communication Components",
    "Reactor": "Reactor Components",
    "Thrust": "Thrust Components",
}


def load_localization(data_dir: str) -> dict[str, str]:
    root = ET.parse(os.path.join(data_dir, "Localization", "MyTexts.resx")).getroot()
    return {node.get("name"): (node.findtext("value") or "") for node in root.iter("data")}


def localize(texts: dict[str, str], key: str) -> str:
    return " ".join(texts.get(key, key).split())


def load_component_names(data_dir: str, texts: dict[str, str]) -> dict[str, str]:
    names: dict[str, str] = {}
    root = ET.parse(os.path.join(data_dir, "Components.sbc")).getroot()
    for component in root.iter("Component"):
        component_id = component.find("Id")
        display_name = component.findtext("DisplayName")
        if component_id is not None and display_name:
            names[component_id.findtext("SubtypeId") or ""] = localize(texts, display_name)
    names.update(COMPONENT_NAME_OVERRIDES)
    return names


def load_blocks(data_dir: str, texts: dict[str, str], component_names: dict[str, str]) -> list[dict]:
    blocks: dict[str, dict] = {}

    for path in sorted(glob.glob(os.path.join(data_dir, "CubeBlocks", "*.sbc"))):
        for definition in ET.parse(path).getroot().iter("Definition"):
            block_id = definition.find("Id")
            components = definition.find("Components")
            if block_id is None or components is None:
                continue

            type_id = (block_id.findtext("TypeId") or "").removeprefix("MyObjectBuilder_")
            subtype_id = block_id.findtext("SubtypeId") or ""

            if (definition.findtext("Public") or "true").strip().lower() == "false" and not is_buildable_wheel(type_id, subtype_id):
                continue
            grid_size = definition.findtext("CubeSize") or ""

            totals: collections.Counter[str] = collections.Counter()
            for component in components.findall("Component"):
                subtype = component.get("Subtype") or ""
                if subtype not in component_names:
                    sys.exit(f"Unknown component '{subtype}' on {type_id}/{subtype_id} in {os.path.basename(path)}")
                totals[component_names[subtype]] += int(component.get("Count") or 0)

            dlcs = [dlc.text.strip() for dlc in definition.findall("DLC") if dlc.text and dlc.text.strip()]

            entry = {
                "Id": f"{type_id}/{subtype_id}/{grid_size}",
                "DisplayName": localize(texts, definition.findtext("DisplayName") or subtype_id),
                "GridSize": grid_size,
                "TypeId": type_id,
                "SubtypeId": subtype_id,
                "Components": [
                    {"ComponentName": name, "Count": count}
                    for name, count in sorted(totals.items(), key=lambda item: item[0].lower())
                    if count > 0
                ],
            }
            if dlcs:
                entry["DlcName"] = ", ".join(sorted(set(dlcs), key=dlcs.index))

            blocks[entry["Id"]] = entry

    disambiguate_labels(list(blocks.values()))
    return sorted(blocks.values(), key=lambda b: (b["DisplayName"].lower(), b["GridSize"], b["Id"].lower()))


def is_buildable_wheel(type_id: str, subtype_id: str) -> bool:
    """Wheels are hidden from the G menu but still get built when added to a suspension.

    Mirrored variants cost the same as the regular wheel, so only the regular one is listed.
    """
    return type_id == "Wheel" and "mirrored" not in subtype_id.lower()


def disambiguate_labels(blocks: list[dict]) -> None:
    """The UI picks blocks by "<DisplayName> (<Grid> Grid)", so every label must be unique."""
    groups: dict[tuple[str, str], list[dict]] = collections.defaultdict(list)
    for block in blocks:
        groups[(block["DisplayName"].lower(), block["GridSize"])].append(block)

    for group in groups.values():
        if len(group) < 2:
            continue
        for block in group:
            block["DisplayName"] = f"{block['DisplayName']} [{block['SubtypeId'] or block['TypeId']}]"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("game_dir", help="Space Engineers install folder (contains Content/Data)")
    parser.add_argument("--output", default=DEFAULT_OUTPUT)
    args = parser.parse_args()

    data_dir = os.path.join(args.game_dir, "Content", "Data")
    texts = load_localization(data_dir)
    blocks = load_blocks(data_dir, texts, load_component_names(data_dir, texts))

    with open(args.output, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(blocks, handle, indent=2, ensure_ascii=False)
        handle.write("\n")

    dlc_count = sum(1 for block in blocks if "DlcName" in block)
    print(f"Wrote {len(blocks)} blocks ({dlc_count} DLC) to {os.path.relpath(args.output, REPO_ROOT)}")


if __name__ == "__main__":
    main()
