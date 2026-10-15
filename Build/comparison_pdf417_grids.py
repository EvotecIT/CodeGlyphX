"""Validate sampled PDF417 grid costs against their frozen proof owner."""
import hashlib
import json
from comparison_photograph_recovery import project_photograph_recovery
from comparison_qualification_history import resolve_proof_owner
from comparison_retained_contexts import project_packet_costs


def project_pdf417_grids(root, qualification):
    projected = project_photograph_recovery(root, qualification,
        'Pdf417GridSamplingProof', 'pdf417-grid-', {'net10':15, 'net8':15, 'net472':9}, 193, (4, 0, 4),
        'PDF417 sampled grids and complete scan costs',
        'Fifteen modern-framework sampling, coordinate and cancellation cases pass; nine apply to .NET Framework. '
        'The exact managed core and test assembly also pass 193 native ARM64 functional checks, including four PDF417 offset, Macro and bottom-up cases, plus 20 Code128 pixel-API calls.')
    if not projected:
        return None
    proof, _, _ = resolve_proof_owner(root, qualification, 'Pdf417GridSamplingProof')
    kinds = {'pdf-positive':38, 'pdf-negative':9, 'qr':1}
    rows = proof['ExtraPackets']
    if (len(rows) != 6 or {(r['Kind'],r['Domain']) for r in rows} !=
            {(kind,domain) for kind in kinds for domain in ('ccd0','ccd1')}
            or any(r['Requests'] != kinds[r['Kind']] for r in rows)):
        raise ValueError('PDF417 or QR repeated workload matrix differs')
    def checked(row):
        name = row['File']
        if '/' in name or '\\' in name:
            raise ValueError('PDF417 manifest leaves its evidence root')
        raw = (root / name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row['Sha256']:
            raise ValueError('PDF417 extra manifest changed')
        return json.loads(raw.decode('utf-8-sig'))
    manifests = proof['ExtraManifests']
    if set(manifests) != set(kinds):
        raise ValueError('PDF417 extra manifest matrix differs')
    inputs = {kind: checked(row) for kind, row in manifests.items()}
    if any(row['ManifestSha256'] != manifests[row['Kind']]['OriginalInputSha256'] for row in rows):
        raise ValueError('PDF417 measured manifest differs from its retained input')
    if inputs['pdf-positive'] != json.loads((root / 'pdf417-supplemental-manifest.json').read_text(encoding='utf-8-sig')):
        raise ValueError('PDF417 supplemental selection changed')
    diagnostic = json.loads((root / 'pdf417-grid-profile-inputs.json').read_text(encoding='utf-8-sig'))
    expected_negatives = [dict(row, Format='Pdf417') for row in diagnostic['Samples']]
    if inputs['pdf-negative']['Samples'] != expected_negatives:
        raise ValueError('Typed PDF417 negative images or empty oracles changed')
    frozen = checked(proof['FrozenCorpusManifest'])['Samples']
    for row in inputs['qr']['Samples']:
        if not any(all(row[k] == sample[k] for k in ('Id','File','Sha256','Expected')) for sample in frozen):
            raise ValueError('QR control image or oracle differs from frozen corpus')
    if len(proof['ExtraHostLoads']) != 6:
        raise ValueError('PDF417 workload load snapshots missing')
    for name, row in proof['ExtraHostLoads'].items():
        if '/' in name or '\\' in name or row['File'] != name:
            raise ValueError('PDF417 load evidence leaves its evidence root')
        raw = (root / name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row['Sha256']:
            raise ValueError('PDF417 initial load snapshot changed')
        capture = json.loads(raw.decode('utf-8-sig'))
        if len(capture['Samples']) != 3 or capture['Masks'] != ['0xFFFF','0xFFFF0000']:
            raise ValueError('PDF417 initial load capture differs')
    extra_costs = project_packet_costs(root, proof, rows)
    order = {'pdf-negative':0, 'pdf-positive':1, 'qr':2}
    projected['costs'] = sorted(extra_costs, key=lambda row: (order[row['kind']], row['domain'])) + projected['costs']
    targeted = [r for r in projected['costs'] if r['kind'] == 'pdf-negative']
    lower = sum(float(r['ratio']) < 1 for r in targeted)
    changes = {kind:[] for kind in kinds}
    allocation = []
    for row in rows:
        packet = json.loads((root / row['PacketFile']).read_text(encoding='utf-8-sig'))
        lanes = {summary['Engine']:summary for summary in packet['Summary']}
        before, current = lanes['Baseline'], lanes['Candidate']
        changes[row['Kind']].append(100 * (current['MedianMs'] / before['MedianMs'] - 1))
        if row['Kind'] == 'pdf-negative':
            allocation.append(100 * (1 - current['Metrics']['AllocatedBytes'] / before['Metrics']['AllocatedBytes']))
    savings = [-value for value in changes['pdf-negative']]
    if lower == 2:
        cost_note = f'The nine PDF417-only negative requests take {min(savings):.1f}–{max(savings):.1f}% less time'
    else:
        cost_note = f'The nine PDF417-only negative requests have lower medians in {lower}/2 domains'
    qr = changes['qr']
    qr_note = (f'The QR control is {min(qr):.1f}–{max(qr):.1f}% slower.' if min(qr) > 0 else
               f'QR-control medians change by {min(qr):+.1f}% to {max(qr):+.1f}%.')
    allocation_note = (f'{min(allocation):.1f}%' if round(min(allocation), 1) == round(max(allocation), 1) else
                       f'{min(allocation):.1f}–{max(allocation):.1f}%')
    projected['summary'] = (
        'PDF417 samples candidate cells as needed. ' + cost_note +
        f' and allocate {allocation_note} fewer managed bytes across the two CPU domains. ' + qr_note)
    projected['completion_note'] = (
        'Typed diagnostic costs are separate from default all-format completion. '
        'The tables retain 38 valid PDF417 images, every measured slowdown and every unfinished scan.')
    projected['limits'] += ' ' + proof['HostLoadCoverage']
    return projected
