"""Validate exact PNG ownership, stream repairs and matched generation evidence."""
import hashlib
import json
import math
import statistics
from comparison_photograph_recovery import project_photograph_recovery
from comparison_qualification_history import resolve_proof_owner


def project_png_output(root, qualification):
    projected = project_photograph_recovery(root, qualification,
        'PngOutputOwnershipProof', 'png-output-', 24, 171, (24, 4, 20),
        'PNG output allocation, stream correctness and complete scan costs',
        'Twenty-four PNG buffer, ownership and stream cases pass on each framework. Against the exact preceding core, twenty pass and four stream regressions fail. The exact core and test assembly pass 171 native ARM64 checks, plus 20 Code128 pixel-API calls. Stored and compressed translucent QR streams preserve every expected pixel after synthetic pool contamination.')
    if not projected:
        return None
    proof, owner, _ = resolve_proof_owner(root, qualification, 'PngOutputOwnershipProof')
    projected.update(project_png_buffers_and_generation(root, proof, owner))
    return projected


def project_png_buffers_and_generation(root, proof, owner):
    """Validate the PNG and generation evidence independently of decoder scan costs."""
    def checked(row):
        name = row['File']
        if '/' in name or '\\' in name:
            raise ValueError('PNG evidence must remain in its evidence root')
        raw = (root / name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row['Sha256']:
            raise ValueError('PNG evidence bytes differ')
        return json.loads(raw.decode('utf-8-sig'))

    buffers = proof['OutputBuffers']
    before, current = checked(buffers['Before']), checked(buffers['Current'])
    if (before['CoreSha256'] != proof['PreviousCoreSha256'] or current['CoreSha256'] != proof['CoreSha256']
            or before['Source'].split('+')[-1] != proof['PreviousSource'] or current['Source'].split('+')[-1] != proof['Source']
            or len(before['Rows']) != 7 or len(current['Rows']) != 7 or len(buffers['Rows']) != 7):
        raise ValueError('PNG buffer source, core or case count differs')
    expected_paths = {'qr-gray1-direct', 'qr-indexed1-direct', 'qr-rgba-direct', 'qr-facade-owned-array',
        'barcode-facade-owned-array', 'matrix-facade-owned-array', 'rgba-image-direct'}
    if {r['Name'] for r in before['Rows']} != expected_paths:
        raise ValueError('PNG public output path differs')
    buffer_rows = []
    for a, b, reported in zip(before['Rows'], current['Rows'], buffers['Rows'], strict=True):
        keys = ('Name', 'Outputs', 'PngBytes', 'PngSha256', 'PixelsSha256', 'Width', 'Height')
        reduction = 100 * (1 - b['ManagedAllocatedBytes'] / a['ManagedAllocatedBytes'])
        if (any(a[k] != b[k] for k in keys) or a['Outputs'] != 100
                or not 0 < b['ManagedAllocatedBytes'] < a['ManagedAllocatedBytes']
                or reported['Case'] != a['Name'] or reported['BeforeAllocatedBytes'] != a['ManagedAllocatedBytes']
                or reported['CurrentAllocatedBytes'] != b['ManagedAllocatedBytes']
                or not math.isclose(reported['ReductionPercent'], reduction, abs_tol=1e-10)):
            raise ValueError('PNG bytes, pixels, cardinality or allocation projection differs')
        buffer_rows.append({'case': a['Name'], 'before_kib': f"{a['ManagedAllocatedBytes']/1024:.2f}",
            'current_kib': f"{b['ManagedAllocatedBytes']/1024:.2f}", 'reduction': f'{reduction:.1f}%'})
    stream = proof['StreamRepair']
    old, new = checked(stream['Before']), checked(stream['Current'])
    if (old['CoreSha256'] != proof['PreviousCoreSha256'] or new['CoreSha256'] != proof['CoreSha256']
            or old['Source'].split('+')[-1] != proof['PreviousSource'] or new['Source'].split('+')[-1] != proof['Source']
            or len(old['Rows']) != 2 or len(new['Rows']) != 2
            or old['Rows'][0]['Compression'] != 0 or old['Rows'][0]['Decoded']
            or old['Rows'][1]['Compression'] != 6 or not old['Rows'][1]['Decoded']
            or old['Rows'][1]['ExactPixels'] or old['Rows'][1]['SyntheticSentinelBytes'] <= 0
            or {r['Compression'] for r in new['Rows']} != {0, 6}
            or any(not r['Decoded'] or not r['ExactPixels'] or r['MismatchedBytes'] or r['SyntheticSentinelBytes']
                   or r['ExpectedSha256'] != r['ActualSha256'] for r in new['Rows'])):
        raise ValueError('Public PNG stream reproduction or repair differs')
    generation = proof['GenerationBeforeCurrent']
    if generation['RecipeSha256'] != owner['GenerationRecipeSha256'] or len(generation['Packets']) != 4:
        raise ValueError('Generation recipe or binary lane missing')
    packets = {}
    for row in generation['Packets']:
        domain, lane = row['Domain'], row['Lane']
        if domain not in ('ccd0', 'ccd1') or lane not in ('before', 'current') or (domain, lane) in packets:
            raise ValueError('Generation lane or placement differs')
        data = checked(row); m = data['Metadata']
        source, core, host = (proof['PreviousSource'], proof['PreviousCoreSha256'], proof['PreviousHostSha256']) if lane == 'before' else (proof['Source'], proof['CoreSha256'], proof['HostSha256'])
        if (row['Source'] != source or row['CoreSha256'] != core or row['HostSha256'] != host
                or m['benchmark.CoreAssemblySha256'] != core or m['benchmark.ComparisonAssemblySha256'] != host
                or m['gitSha'] != proof['Source'] or m['gitWorktreeClean'] != 'true'
                or m['benchmark.Workload'] != 'fixed-version-m-utf8-v1' or m['outlierMode'] != 'None'
                or m['processAffinityMask'] != ('0xFFFF' if domain == 'ccd0' else '0xFFFF0000') or m['processPriority'] != 'Normal'
                or len(data['Summary']) != 24 or {r['Engine'] for r in data['Summary']} != {'CodeGlyphX', 'ZXing.Net', 'ZXing-C++', 'QRCoder'}
                or {r['Scenario'] for r in data['Summary']} != {'Url', 'Numeric', 'Unicode'}
                or {r['Operation'] for r in data['Summary']} != {'Modules', 'Png'}
                or any(r['Status'] != 'Succeeded' or r['FailureCount'] or r['SampleCount'] != 12 or r['Metrics']['Outputs'] != 100 for r in data['Summary'])):
            raise ValueError('Generation binary, recipe, execution or output count differs')
        for summary in data['Summary']:
            measured = [r for r in data['Samples'] if r['Engine'] == summary['Engine'] and r['Scenario'] == summary['Scenario'] and r['Operation'] == summary['Operation'] and r['Iteration'] >= 0]
            if len(measured) != 12 or any(r['Metrics']['Outputs'] != 100 or r['Status'] not in (0, 'Succeeded') for r in measured):
                raise ValueError('Generation measured output cardinality differs')
            allocation_mean = statistics.mean(r['Metrics']['ManagedAllocatedBytes'] for r in measured)
            median_ms = statistics.median(r['DurationMs'] for r in measured)
            if (not math.isclose(summary['Metrics']['ManagedAllocatedBytes'], allocation_mean, rel_tol=1e-10, abs_tol=1e-8)
                    or not math.isclose(summary['MedianMs'], median_ms, rel_tol=1e-10, abs_tol=1e-8)):
                raise ValueError('Generation summary differs from retained measurements')
        packets[domain, lane] = data
    generation_rows = []
    qr_reductions = []
    qr_faster = 0
    for domain in ('ccd0', 'ccd1'):
        order = ['before', 'current'] if domain == 'ccd0' else ['current', 'before']
        if (generation['SourceOrder'][domain] != order
                or packets[domain, order[0]]['StartedUtc'] >= packets[domain, order[1]]['StartedUtc']):
            raise ValueError('Generation source order differs from actual launches')
        def key(r): return r['Scenario'], r['Operation'], r['Engine']
        a = {key(r): r for r in packets[domain, 'before']['Summary']}
        b = {key(r): r for r in packets[domain, 'current']['Summary']}
        if len(a) != 24 or set(a) != set(b):
            raise ValueError('Generation scenario/engine matrix differs')
        for k in sorted(a):
            x, y = a[k], b[k]
            if x['Variables'] != y['Variables']:
                raise ValueError('Generation payload or fixed version differs')
            if k[1] == 'Png' and k[2] == 'CodeGlyphX':
                qr_reductions.append(100 * (1 - y['Metrics']['ManagedAllocatedBytes'] / x['Metrics']['ManagedAllocatedBytes']))
                qr_faster += y['MedianMs'] < x['MedianMs']
            generation_rows.append({'domain': domain.upper(), 'case': k[0], 'operation': k[1], 'engine': k[2],
                'before_ms': f"{x['MedianMs']:.3f}", 'current_ms': f"{y['MedianMs']:.3f}",
                'ratio': f"{y['MedianMs']/x['MedianMs']:.3f}",
                'before_kib': f"{x['Metrics']['ManagedAllocatedBytes']/1024:.2f}",
                'current_kib': f"{y['Metrics']['ManagedAllocatedBytes']/1024:.2f}"})
    return dict(buffer_rows=buffer_rows, buffer_scope=buffers['Scope'], stream_scope=stream['Scope'],
        generation_rows=generation_rows, generation_scope=generation['Scope'], generation_limits=generation['Limits'],
        qr_png_allocation_reduction=f'{min(qr_reductions):.1f}–{max(qr_reductions):.1f}%',
        qr_png_timing_note=f'CodeGlyphX PNG medians are lower in {qr_faster}/6 payload/domain cases and higher in {6-qr_faster}/6. Shared-host load and separate launches limit causal latency claims.')


def append_png_output_section(lines, projected):
    if not projected:
        return
    lines += ['', '## PNG output allocation and stream correctness', '', projected['changes'], '', projected['contracts'], '',
        'Seven public rendering probes preserve identical PNG bytes and decoded pixels against the preceding binary. Allocation counters cover 100 outputs after five priming calls; these probes do not rank elapsed time.', '',
        '| Public path | Previous managed KiB / 100 outputs | Current managed KiB / 100 outputs | Reduction |',
        '| --- | ---: | ---: | ---: |']
    for r in projected['buffer_rows']:
        lines.append(f"| {r['case']} | {r['before_kib']} | {r['current_kib']} | {r['reduction']} |")
    lines += ['', projected['stream_scope'], '',
        f"The matched generation matrix reduces CodeGlyphX managed PNG allocation by {projected['qr_png_allocation_reduction']} across all six payload/domain cases.", '',
        projected['qr_png_timing_note'], '', projected['generation_scope'], '', projected['generation_limits'], '',
        '| Domain | Payload | Output | Engine | Previous median ms | Current median ms | Current / previous | Previous managed KiB | Current managed KiB |',
        '| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: |']
    for r in projected['generation_rows']:
        lines.append(f"| {r['domain']} | {r['case']} | {r['operation']} | {r['engine']} | {r['before_ms']} | {r['current_ms']} | {r['ratio']} | {r['before_kib']} | {r['current_kib']} |")
    lines += ['', '[PNG proof and all raw packets](evidence/png-output-proof.json).']
