"""Validate scalar retail digit compatibility and complete image comparisons."""
import hashlib
import json
import statistics
from comparison_photograph_recovery import project_photograph_recovery
from comparison_qualification_history import resolve_proof_owner


def project_retail_digit_fits(root, qualification):
    projected = project_photograph_recovery(root, qualification,
        'RetailDigitFitProof', 'retail-digit-', 3, 174, (3, 0, 3),
        'Retail digit fitting and complete scan costs',
        'Three scalar-reference parity cases pass on each framework and against the preceding core. '
        'Each case checks acceptance, selected pattern and exact returned fit against explicitly written EAN patterns. '
        'The exact managed core and test assembly pass 174 native ARM64 checks, plus 20 Code128 pixel-API calls.')
    if not projected:
        return None
    proof, _, _ = resolve_proof_owner(root, qualification, 'RetailDigitFitProof')
    reference = proof['ScalarReference']
    if (reference['ComparisonsPerParity'] != 40736 or reference['ParityCases'] != 3
            or reference['ExhaustiveSmallVectorsPerParity'] != 20736
            or reference['LargerWidthIterationsPerParity'] != 20000
            or reference['DistinctLargerVectorsPerParity'] != 1024):
        raise ValueError('Scalar-reference coverage differs')
    projected['contracts'] += ' ' + reference['Scope']
    targeted = [r for r in projected['costs'] if r['kind'] in ('retail-positive', 'retail-negative', 'whole')]
    if len(targeted) != 6:
        raise ValueError('Missing retail or whole-image cost domain')
    lower = sum(float(r['ratio']) < 1 for r in targeted)
    projected['completion_note'] = (
        f'The six targeted workload/domain comparisons have lower medians in {lower}/6 cases. '
        'The retail-positive workload has 87 typed EAN/UPC requests, the retail-negative workload has nine typed requests, '
        'and the mixed workload has 19 requests. Small differences on this shared host do not establish reliable negative-search completion.')
    rejected = proof['RejectedResidualTable']
    if (rejected['Source'] == proof['Source'] or len(rejected['Packets']) != 2
            or rejected['PreviousCoreSha256'] != proof['PreviousCoreSha256']):
        raise ValueError('Rejected draft identity differs')
    for row in rejected['Packets']:
        name = row['File']
        if '/' in name or '\\' in name:
            raise ValueError('Rejected draft evidence must stay in its evidence root')
        raw = (root / name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row['Sha256']:
            raise ValueError('Rejected draft evidence changed')
        packet = json.loads(raw.decode('utf-8-sig'))
        metadata = packet['Metadata']
        if (metadata['benchmark.CandidateSource'] != rejected['Source']
                or metadata['benchmark.CandidateCoreSha256'] != rejected['CoreSha256']
                or metadata['benchmark.BaselineCoreSha256'] != rejected['PreviousCoreSha256']
                or len(packet['Summary']) != 2
                or {s['Engine'] for s in packet['Summary']} != {'Baseline', 'Candidate'}
                or any(s['Status'] != 'Succeeded' or s['FailureCount'] for s in packet['Summary'])):
            raise ValueError('Rejected draft source or binary differs')
        lanes = {s['Engine']:s for s in packet['Summary']}
        if lanes['Candidate']['MedianMs'] <= lanes['Baseline']['MedianMs']:
            raise ValueError('Rejected draft slowdown differs from retained measurements')
        for summary in packet['Summary']:
            measured = [s for s in packet['Samples'] if s['Engine'] == summary['Engine'] and s['Iteration'] >= 0]
            if (len(measured) != row['Measurements'] or summary['SampleCount'] != row['Measurements']
                    or summary['MedianMs'] != statistics.median(s['DurationMs'] for s in measured)):
                raise ValueError('Rejected draft sample count or median differs')
    projected['exploration_note'] += ' ' + rejected['Scope']
    projected['limits'] += ' ' + proof['HostLoadCoverage']
    return projected
