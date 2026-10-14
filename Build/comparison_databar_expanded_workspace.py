"""Project Expanded workspaces, exact character fits, ordered rows and complete-scan costs."""
import hashlib
import json
import math
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
    native = read('native-arm64')
    if (native != proof['NativeArm64'] or native['Source'] != proof['Source'] or native['CoreSha256'] != proof['CoreSha256']
            or native['InputFiles']['CodeGlyphX.dll'] != proof['CoreSha256']
            or native['InputFiles']['CodeGlyphX.Tests.dll'] != proof['TestAssemblySha256']
            or native['Architecture'] != 'arm64' or native['FrameworkRollForward'] != 'Disable'
            or not native['SDKVersion'] or not native['SelectedFrameworkVersion']
            or native['DecisionProof']['Status'] != 'passed' or native['DecisionProof']['Checks'] != 83
            or len(native['DecisionProof']['Cases']) != 83 or any(r['Status'] != 'passed' for r in native['DecisionProof']['Cases'])
            or native['DecisionProof']['CoreSha256'] != proof['CoreSha256']
            or native['DecisionProof']['TestAssemblySha256'] != proof['TestAssemblySha256']
            or native['DecisionProof']['CoreInformationalVersion'].split('+')[-1] != proof['Source']):
        raise ValueError('Workspace native proof differs from its exact core or tests')
    profile_inputs = checked(proof['ProfileManifest'])
    if (proof['FrozenCorpusManifest']['OriginalInputSha256'] != owner['CorpusManifestSha256']
            or proof['FrozenNegativeManifest']['OriginalInputSha256'] != owner['NegativeManifestSha256']):
        raise ValueError('Workspace frozen manifests differ from their qualification')
    frozen_samples = {r['Id']:r for m in (frozen,negatives) for r in m['Samples']}
    ids = {r['Id'] for r in profile_inputs['Samples']}
    if len(ids) != 9 or any(frozen_samples.get(r['Id']) != r for r in profile_inputs['Samples']):
        raise ValueError('Profile image hashes, oracles or hints differ from the frozen corpus')
    diagnostic = read('allocation-diagnostic')
    if (diagnostic != proof['AllocationDiagnostic'] or diagnostic['Source'] != proof['Source']
            or diagnostic['CoreSha256'] != proof['CoreSha256'] or diagnostic['PreviousCoreSha256'] != proof['PreviousCoreSha256']
            or diagnostic['Images'] != 9 or diagnostic['PrimingCalls'] != 9 or diagnostic['MeasuredCalls'] != 27):
        raise ValueError('Module-run allocation diagnostic identity differs')
    packets = proof['DiagnosticPackets']
    if len(packets) != 2 or {r['Lane'] for r in packets} != {'before','after'}:
        raise ValueError('Module-run allocation diagnostic lane missing')
    means = {}
    completions = {}
    for row in packets:
        data = checked(row); profile = checked(row,'ProfileFile','ProfileSha256')
        source, core = (proof['PreviousSource'],proof['PreviousCoreSha256']) if row['Lane']=='before' else (proof['Source'],proof['CoreSha256'])
        if (row['Source'] != source or row['CoreSha256'] != core or data['CoreSha256'] != core
                or len(data['Rows']) != 36 or {(r['Id'],r['Iteration']) for r in data['Rows']} != {(id,i) for id in ids for i in (-1,0,1,2)}
                or any(len(r['Outcomes']) != 1 or r['Outcomes'][0]['Id'] != r['Id'] or r['Outcomes'][0]['Engine'] != 'CodeGlyphX' for r in data['Rows'])
                or profile['Source'] != source or profile['CoreSha256'] != core or profile['ProbeOutcomeSha256'] != row['Sha256']
                or len(profile['DerivedFromTraceSha256']) != 64 or profile['AllocationTicks'] < 1 or profile['ThreadTimeSamples'] < 1):
            raise ValueError('Incomplete module-run profile identities or actual outcomes')
        measured = [r for r in data['Rows'] if r['Iteration'] >= 0]
        means[row['Lane']] = sum(r['AllocatedBytes'] for r in measured)/27
        completions[row['Lane']] = sum(r['Outcomes'][0]['Status']=='Correct' for r in measured)
    if (not math.isclose(means['before'],diagnostic['BeforeMeanAllocatedBytes'],abs_tol=1e-8)
            or not math.isclose(means['after'],diagnostic['CurrentMeanAllocatedBytes'],abs_tol=1e-8)
            or not math.isclose(means['after']/means['before'],diagnostic['AllocationRatio'],abs_tol=1e-12)):
        raise ValueError('Module-run diagnostic counters differ')
    supplemental = proof['SupplementalPackets']
    kinds = {'databar-positive':12,'databar-negative':31,'expanded-positive':30,'expanded-negative':31}
    if (len(supplemental) != 8 or {(r['Kind'],r['Domain']) for r in supplemental} !=
            {(kind,domain) for kind in kinds for domain in ('ccd0','ccd1')}
            or any(r['Requests'] != kinds[r['Kind']] or r['ManifestSha256'] !=
                   proof['ExpandedDiagnostics' if r['Kind'].startswith('expanded') else 'Diagnostics'][r['Kind'].split('-')[1]]['ManifestSha256'] for r in supplemental)):
        raise ValueError('Supplemental DataBar/Expanded workload differs')
    if set(proof['ExpandedManifests']) != {'positive','negative'}:
        raise ValueError('Expanded diagnostic manifest missing')
    for kind,row in proof['ExpandedManifests'].items():
        manifest = checked(row)
        if row['OriginalInputSha256'] != proof['ExpandedDiagnostics'][kind]['ManifestSha256']:
            raise ValueError('Expanded diagnostic input identity differs')
        expected = {r['Id']:r for r in frozen['Samples'] if r['Format'] in ('Gs1DataBarExpanded','Gs1DataBarExpandedStacked')}
        if kind == 'negative': expected = {r['Id']:dict(r,Format='Gs1DataBarExpandedStacked') for r in frozen_samples.values() if not r['Expected']}
        if len(manifest['Samples']) != (30 if kind == 'positive' else 31) or {r['Id']:r for r in manifest['Samples']} != expected:
            raise ValueError('Expanded diagnostic frozen images, hints or oracles differ')
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
