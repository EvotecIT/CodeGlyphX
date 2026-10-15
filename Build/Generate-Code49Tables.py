"""Generate Code 49's normative ANSI/AIM tables from a pinned Zint reference header.

The generated data is checked in; normal builds do not require Python or Zint.
Usage: python Build/Generate-Code49Tables.py header-path output-path source-revision
"""
import re
import sys
from pathlib import Path


def array(source, name):
    match = re.search(r'\b' + re.escape(name) + r'\s*(?:\[[^]]*\])+\s*=\s*\{(.*?)\n\};', source, re.S)
    if not match:
        raise ValueError(f'Missing normative table {name}')
    return re.sub(r'/\*.*?\*/', '', match.group(1), flags=re.S)


def emit_numbers(lines, name, values, kind):
    lines.append(f'    internal static readonly {kind}[] {name} = {{')
    for start in range(0, len(values), 12):
        lines.append('        ' + ', '.join(values[start:start + 12]) + ',')
    lines.append('    };')


header, output, revision = sys.argv[1:]
if not re.fullmatch(r'[0-9a-f]{40}', revision):
    raise ValueError('A complete pinned source revision is required')
source = Path(header).read_text(encoding='utf-8')
notice = re.search(r'/\*\n(.*?)\*/', source, re.S).group(0)
lines = [
    '// Generated normative data from ANSI/AIM-BC6-2000 as published by Zint.',
    f'// Source: https://github.com/zint/zint/blob/{revision}/backend/code49.h',
    '// Regenerate with Build/Generate-Code49Tables.py; do not edit table values.',
    notice, '// SPDX-License-Identifier: BSD-3-Clause',
    'namespace CodeGlyphX.Code49;', '',
    '/// <summary>Normative Code 49 character, parity, and symbol-check tables.</summary>',
    'internal static class Code49Tables {',
]
tokens = re.findall(r'\{([^{}]+)\}', array(source, 'c49_table7'))
ascii_map = []
for token in tokens:
    characters = re.findall(r"'([^']*)'|\"([^\"]*)\"", token)
    ascii_map.append(''.join(first or second for first, second in characters))
if len(ascii_map) != 128 or any(not value for value in ascii_map):
    raise ValueError('The ASCII mapping is incomplete')
lines.append('    internal static readonly string[] Ascii = {')
for start in range(0, 128, 8):
    lines.append('        ' + ', '.join('"' + value + '"' for value in ascii_map[start:start + 8]) + ',')
lines.append('    };')
for source_name, target_name in [('c49_x_weight', 'XWeights'), ('c49_y_weight', 'YWeights'), ('c49_z_weight', 'ZWeights')]:
    values = re.findall(r'\d+', array(source, source_name))
    if len(values) != 32:
        raise ValueError('Unexpected check-weight table length')
    emit_numbers(lines, target_name, values, 'int')
parities = [''.join(re.findall(r"'([OE])'", token)) for token in re.findall(r'\{([^{}]+)\}', array(source, 'c49_table4'))]
if len(parities) != 8 or any(len(value) != 4 for value in parities):
    raise ValueError('Unexpected row-parity table')
lines.append('    internal static readonly string[] RowParity = { ' + ', '.join('"' + value + '"' for value in parities) + ' };')
for source_name, target_name in [('c49_even_bitpattern', 'Even'), ('c49_odd_bitpattern', 'Odd')]:
    values = re.findall(r'0x[0-9A-Fa-f]+', array(source, source_name))
    if len(values) != 2401 or len(set(values)) != len(values):
        raise ValueError('Unexpected symbol-pattern table')
    emit_numbers(lines, target_name, values, 'ushort')
lines.append('}')
Path(output).write_text('\n'.join(lines) + '\n', encoding='utf-8')
