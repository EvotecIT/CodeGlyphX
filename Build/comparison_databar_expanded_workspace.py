"""Project Expanded workspaces, exact character fits, ordered rows and complete-scan costs."""
import hashlib
import json
from comparison_decoder_contracts import validate_native_contracts, validate_profile_diagnostics, validate_expanded_packets
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def project_databar_expanded_workspace(root, qualification):
    proof, owner, historical = resolve_proof_owner(root, qualification, 'DataBarExpandedWorkspaceProof')
    if not proof:
        return None

    def read(name):
        return json.loads((root / ('databar-expanded-workspace-' + name + '.json')).read_text(encoding='utf-8-sig'))

    def checked(row, key='File', digest='Sha256'):
        name = row[key]
        if '/' in name or '\\' in name:
            raise ValueError('Workspace proof file must stay in its evidence root')
        raw = (root / name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row[digest]:
            raise ValueError('Expanded Stacked workspace evidence bytes differ')
        return json.loads(raw.decode('utf-8-sig'))

    if read('proof') != proof or proof['Contracts'] != {'net10':5, 'net8':5, 'net472':5}:
        raise ValueError('Expanded Stacked workspace download or framework contracts differ')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('Workspace review does not cover its source')
    eq = checked(proof['NormalizationEquivalence'])
    if (eq['Status'] != 'Passed' or eq['PreviousCoreSha256'] != proof['PreviousCoreSha256']
            or eq['CandidateCoreSha256'] != proof['CoreSha256'] or eq['Cases'] != 24616
            or len(eq['Rows']) != 24616 or eq['Accepted'] != 23443 or eq['Rejected'] != 1173
            or sum(r['Success'] for r in eq['Rows']) != eq['Accepted']
            or proof['NormalizationEquivalence']['Cases'] != eq['Cases']
            or proof['NormalizationEquivalence']['Accepted'] != eq['Accepted']
            or proof['NormalizationEquivalence']['Rejected'] != eq['Rejected']):
        raise ValueError('Actual character-fit success, width or fit-bit equivalence differs')
    if any(len(r['Widths']) != (10 if r['Success'] else 2) or r['Widths'][:2] != [2,1]
           or len(r['FitBits']) != 16 or any(c not in '0123456789ABCDEF' for c in r['FitBits']) for r in eq['Rows']):
        raise ValueError('Character-fit prefix, appended widths or exact fit bits differ')
    row_proof = proof['RowCollectorEquivalence']
    rows = checked(row_proof)
    frozen = checked(proof['FrozenCorpusManifest'])
    negatives = checked(proof['FrozenNegativeManifest'])
    collector_inputs = {r['Id']:r['Sha256'].upper() for m in (frozen,negatives) for r in m['Samples']
                        if not r['Expected'] or r['Format'] in ('Gs1DataBarExpanded','Gs1DataBarExpandedStacked')}
    variants = {(r['Id'],r['Rotated'],r['Inverted']) for r in rows['Records']}
    if (rows['Status'] != 'Passed' or rows['PreviousCoreSha256'] != proof['PreviousCoreSha256']
            or rows['CandidateCoreSha256'] != proof['CoreSha256'] or rows['Images'] != 61
            or rows['Variants'] != len(rows['Records']) or rows['Variants'] != 244
            or rows['PatternCount'] != 124 or rows['Rows'] != 45113 or rows['CollectorCallsPerBinary'] != 564768
            or not rows['DirtyIntPoolSeededBeforeEveryVariant']
            or len(collector_inputs) != 61
            or variants != {(id,rotation,inversion) for id in collector_inputs for rotation in (False,True) for inversion in (False,True)}
            or any(r['InputSha256'] != collector_inputs[r['Id']] for r in rows['Records'])
            or any(row_proof[k] != rows[k] for k in ('Images','Variants','Rows','CollectorCallsPerBinary','PatternCount'))):
        raise ValueError('Actual Expanded Stacked row-collector equivalence differs')
    count = calls = 0
    for record in rows['Records']:
        raw = record['DescriptorJson'].encode('utf-8')
        if (hashlib.sha256(raw).hexdigest().upper() != record['DescriptorSha256']
                or record['PreviousDescriptorSha256'] != record['DescriptorSha256']):
            raise ValueError('Ordered row descriptor bytes differ')
        observations = json.loads(raw)
        if [(g['Axis'],g['Reverse'],g['Pattern']) for g in observations] != [(a,r,p) for a in (0,1) for r in (0,1) for p in range(124)]:
            raise ValueError('Ordered Expanded pattern groups differ')
        row_count = sum(len(g['Rows']) for g in observations)
        if row_count != record['Rows'] or any(not 17 <= len(r['Widths']) <= 235 for g in observations for r in g['Rows']):
            raise ValueError('Actual physical row widths or counts differ')
        count += row_count; calls += record['CollectorCalls']
    if count != rows['Rows'] or calls != rows['CollectorCallsPerBinary']:
        raise ValueError('Actual row collector totals differ')
    validate_native_contracts(proof, read('native-arm64'), 83)
    frozen_samples, means, completions = validate_profile_diagnostics(proof, owner, frozen, negatives, checked, read)
    supplemental = validate_expanded_packets(proof, frozen, frozen_samples, checked)
    projected = project_context_costs(root,qualification,'DataBarExpandedWorkspaceProof')
    projected['costs'] += project_packet_costs(root,proof,supplemental)
    losses = []
    for row in proof['Packets'] + supplemental:
        packet = json.loads((root/row['PacketFile']).read_text(encoding='utf-8-sig'))
        counts = {engine:sum(s['Metrics']['CorrectImages'] for s in packet['Samples'] if s['Engine']==engine and s['Iteration']>=0) for engine in ('Baseline','Candidate')}
        if counts['Candidate'] < counts['Baseline']:
            losses.append(f"{row['Kind']} on {row['Domain'].upper()}: {counts['Baseline']:g} to {counts['Candidate']:g} correct requests out of {12*row['Requests']} across twelve measured calls.")
    projected.update(title=('Historical ' if historical else '')+'Expanded Stacked decoding workspace and scan costs',
        anchor='databar-expanded-workspace', evidence='databar-expanded-workspace-proof.json', evidence_label='Expanded Stacked workspace proof',
        contracts='All 24,616 actual character-fitting outcomes match the preceding qualified binary, including success, appended widths and exact fit bits. The image-row collector preserves all 45,113 ordered observations across 244 rotation/polarity variants of 61 frozen images and 564,768 calls per binary, including dirty pooled storage. Five public workspace contracts pass on each framework; the exact core and test assembly pass 83 native ARM64 checks.',
        exploration_note=f"The separate traced nine-image diagnostic records {completions['before']}/27 to {completions['after']}/27 correct rejections after priming, with mean managed allocation {means['before']/1048576:.2f} to {means['after']/1048576:.2f} MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.",
        completion_note='', completion_notes=losses)
    projected['history_note']='These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.'
    return projected
