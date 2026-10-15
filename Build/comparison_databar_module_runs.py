"""Project bounded DataBar module extraction, public outcomes and complete-scan costs."""
import hashlib
import json
from comparison_decoder_contracts import validate_native_contracts, validate_profile_diagnostics
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def project_databar_module_runs(root, qualification):
    proof, owner, historical = resolve_proof_owner(root, qualification, 'DataBarModuleRunProof')
    if not proof:
        return None

    def read(name):
        return json.loads((root / ('databar-module-runs-' + name + '.json')).read_text(encoding='utf-8-sig'))

    def checked(row, key='File', digest='Sha256'):
        name = row[key]
        if '/' in name or '\\' in name:
            raise ValueError('Module-run proof file must stay in its evidence root')
        raw = (root / name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row[digest]:
            raise ValueError('DataBar module-run evidence bytes differ')
        return json.loads(raw.decode('utf-8-sig'))

    if read('proof') != proof or proof['Contracts'] != {'net10':19, 'net8':19, 'net472':19}:
        raise ValueError('DataBar module-run download or framework contracts differ')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('Module-run review does not cover its source')
    eq = checked(proof['ModuleEquivalence'])
    if (eq['Status'] != 'Passed' or eq['PreviousCoreSha256'] != proof['PreviousCoreSha256']
            or eq['CandidateCoreSha256'] != proof['CoreSha256'] or eq['Variants'] != 16985
            or len(eq['Rows']) != 16985 or eq['Accepted'] != 1004 or eq['Rejected'] != 15981
            or sum(r['Success'] for r in eq['Rows']) != eq['Accepted']
            or proof['ModuleEquivalence']['Cases'] != eq['Variants']
            or proof['ModuleEquivalence']['Accepted'] != eq['Accepted']
            or proof['ModuleEquivalence']['Rejected'] != eq['Rejected']):
        raise ValueError('Public module success/text equivalence differs')
    validate_native_contracts(proof, read('native-arm64'), 60)
    frozen = checked(proof['FrozenCorpusManifest'])
    negatives = checked(proof['FrozenNegativeManifest'])
    frozen_samples, means, completions = validate_profile_diagnostics(proof, owner, frozen, negatives, checked, read)
    supplemental = proof['SupplementalPackets']
    if (len(supplemental) != 4 or {(r['Kind'],r['Domain']) for r in supplemental} !=
            {(kind,domain) for kind in ('databar-positive','databar-negative') for domain in ('ccd0','ccd1')}
            or any(r['Requests'] != (12 if r['Kind']=='databar-positive' else 31)
                   or r['ManifestSha256'] != proof['Diagnostics'][r['Kind'].split('-')[1]]['ManifestSha256'] for r in supplemental)):
        raise ValueError('Module-run supplemental DataBar workload differs')
    projected = project_context_costs(root,qualification,'DataBarModuleRunProof')
    projected['costs'] += project_packet_costs(root,proof,supplemental)
    losses = []
    for row in proof['Packets'] + supplemental:
        packet = json.loads((root/row['PacketFile']).read_text(encoding='utf-8-sig'))
        counts = {engine:sum(s['Metrics']['CorrectImages'] for s in packet['Samples'] if s['Engine']==engine and s['Iteration']>=0) for engine in ('Baseline','Candidate')}
        if counts['Candidate'] < counts['Baseline']:
            losses.append(f"{row['Kind']} on {row['Domain'].upper()}: {counts['Baseline']:g} to {counts['Candidate']:g} correct requests out of {12*row['Requests']} across twelve measured calls.")
    projected.update(title=('Historical ' if historical else '')+'DataBar bounded module extraction and scan costs',
        anchor='databar-module-runs', evidence='databar-module-runs-proof.json', evidence_label='DataBar module-run proof',
        contracts='All 16,985 public module-decoder success and text outcomes match the preceding qualified binary, including valid, damaged, trimmed, padded and malformed inputs. Nineteen contracts pass on each framework, covering every supported Expanded character count from 4 through 22, independent Limited vectors, maximum stacked symbols and initialized buffer boundaries. The exact managed binary and test assembly pass 60 native ARM64 checks, including those nineteen DataBar contracts.',
        exploration_note=f"The separate traced nine-image diagnostic records {completions['before']}/27 to {completions['after']}/27 correct rejections after priming, with mean managed allocation {means['before']/1048576:.2f} to {means['after']/1048576:.2f} MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.",
        completion_note='', completion_notes=losses)
    projected['history_note']='These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.'
    return projected
