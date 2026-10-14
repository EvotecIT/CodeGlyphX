"""Project shared linear runs, public module decisions and complete scan costs."""
import hashlib
import json
import math
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def project_linear_run_workspace(root, qualification):
    proof, owner, historical = resolve_proof_owner(root, qualification, 'LinearRunWorkspaceProof')
    if not proof:
        return None

    def read(name):
        return json.loads((root / ('linear-run-workspace-' + name + '.json')).read_text(encoding='utf-8-sig'))

    def checked(row, key='File', digest='Sha256'):
        name = row[key]
        if '/' in name or '\\' in name:
            raise ValueError('Workspace proof file must stay in its evidence root')
        raw = (root / name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row[digest]:
            raise ValueError('Shared linear run evidence bytes differ')
        return json.loads(raw.decode('utf-8-sig'))

    if read('proof') != proof or proof['Contracts'] != {'net10':7, 'net8':7, 'net472':7}:
        raise ValueError('Shared linear run download or framework contracts differ')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('Workspace review does not cover its source')
    eq = checked(proof['RunEquivalence'])
    if (eq['Status'] != 'Passed' or eq['PreviousCoreSha256'] != proof['PreviousCoreSha256']
            or eq['CandidateCoreSha256'] != proof['CoreSha256'] or eq['Cases'] != 133203
            or len(eq['Records']) != 133203 or eq['ExhaustiveBinaryMaximumLength'] != 16
            or eq['SeededRandomCases'] != 2048 or eq['BoundaryCases'] != 84
            or not eq['DirtySharedPoolSeeded'] or eq['NestedWidthMultiplier'] != 2
            or proof['RunEquivalence']['Cases'] != eq['Cases']):
        raise ValueError('Actual run values, coverage or nested ownership differ')
    boundaries = (0,1,2,7,15,16,17,31,32,33,255,256,257,1023,1024,1025,4095,4096,4097,65535,65536)
    expected = {f'exhaustive/{length}/{value}' for length in range(17) for value in range(1 << length)}
    expected |= {f'seeded/{index}' for index in range(2048)}
    expected |= {f'boundary/{length}/{pattern}' for length in boundaries for pattern in range(4)}
    if {r['Id'] for r in eq['Records']} != expected:
        raise ValueError('Run input coverage differs')
    def widths_digest(widths):
        return hashlib.sha256(json.dumps(widths,separators=(',',':')).encode()).hexdigest().upper()
    for row in eq['Records']:
        widths = row['Widths']
        if (not widths or any(type(w) is not int or w < 0 for w in widths)
                or (0 in widths and widths != [0]) or len(row['InputSha256']) != 64
                or widths_digest(widths) != row['WidthsSha256']
                or row['RepeatedWidthsSha256'] != row['WidthsSha256']
                or widths_digest([2*w for w in widths]) != row['NestedWidthsSha256']):
            raise ValueError('Actual run widths or nested/repeated hashes differ')
        parts = row['Id'].split('/')
        bits = None
        if parts[0] == 'exhaustive':
            length,value = map(int,parts[1:])
            bits = bytes((value >> bit) & 1 for bit in range(length))
        elif parts[0] == 'boundary':
            length,pattern = map(int,parts[1:])
            bits = bytes(0 if pattern==0 else 1 if pattern==1 else int(i%2==0) if pattern==2 else int((i//7)%2==0) for i in range(length))
        elif not 1 <= sum(widths) <= 4097:
            raise ValueError('Seeded run input length differs')
        if bits is not None:
            actual = []
            for bit in bits:
                if not actual or bit != last:
                    actual.append(1)
                else:
                    actual[-1] += 1
                last = bit
            if not actual: actual = [0]
            if hashlib.sha256(bits).hexdigest().upper() != row['InputSha256'] or actual != widths:
                raise ValueError('Run widths differ from their exhaustive or boundary input')
    public = checked(proof['PublicModuleEquivalence'])
    records = public['Records']
    if (public['Status'] != 'Passed' or public['PreviousCoreSha256'] != proof['PreviousCoreSha256']
            or public['CandidateCoreSha256'] != proof['CoreSha256'] or public['Cases'] != 22464
            or len(records) != 22464 or [r['Id'] for r in records] != list(range(22464))
            or proof['PublicModuleEquivalence']['Cases'] != public['Cases']):
        raise ValueError('Public module outcomes or exact binary identity differ')
    formats = {'Code128','GS1_128','Codabar','Code11','Plessey','Telepen','ITF','ITF14','Industrial2of5',
               'Matrix2of5','IATA2of5','PatchCode','Pharmacode','Code39','Code93','EAN','UPCA','UPCE','MSI'}
    if ({r['FixtureType'] for r in records} != formats or {r['Transform'] for r in records} != {0,1,2,3}
            or {r['Scale'] for r in records} != {1,2,3} or {r['Padding'] for r in records} != {0,12}
            or {r['Repeat'] for r in records} != {1,3,32} or {r['Checksums'] for r in records} != {False,True}
            or not any(r['Hint'] is None for r in records) or not any(r['Hint']==r['FixtureType'] for r in records)
            or not any(r['Hint'] is not None and r['Hint']!=r['FixtureType'] for r in records)
            or any(r['Length'] < 0 or len(r['InputSha256']) != 64 or type(r['Decision']['Success']) is not bool
                   or type(r['Decision']['ReaderInitialization']) is not bool
                   or (r['Decision']['Success'] and (not isinstance(r['Decision']['Text'],str) or r['Decision']['Type'] not in formats))
                   or (not r['Decision']['Success'] and (r['Decision']['Text'] is not None or r['Decision']['Type'] is not None or r['Decision']['ReaderInitialization'])) for r in records)):
        raise ValueError('Public module policies, input coverage or decision shape differ')
    frozen = checked(proof['FrozenCorpusManifest'])
    negatives = checked(proof['FrozenNegativeManifest'])
    native = read('native-arm64')
    if (native != proof['NativeArm64'] or native['Source'] != proof['Source'] or native['CoreSha256'] != proof['CoreSha256']
            or native['InputFiles']['CodeGlyphX.dll'] != proof['CoreSha256']
            or native['InputFiles']['CodeGlyphX.Tests.dll'] != proof['TestAssemblySha256']
            or native['Architecture'] != 'arm64' or native['FrameworkRollForward'] != 'Disable'
            or not native['SDKVersion'] or not native['SelectedFrameworkVersion']
            or native['DecisionProof']['Status'] != 'passed' or native['DecisionProof']['Checks'] != 90
            or len(native['DecisionProof']['Cases']) != 90 or any(r['Status'] != 'passed' for r in native['DecisionProof']['Cases'])
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
    projected = project_context_costs(root,qualification,'LinearRunWorkspaceProof')
    projected['costs'] += project_packet_costs(root,proof,supplemental)
    losses = []
    for row in proof['Packets'] + supplemental:
        packet = json.loads((root/row['PacketFile']).read_text(encoding='utf-8-sig'))
        counts = {engine:sum(s['Metrics']['CorrectImages'] for s in packet['Samples'] if s['Engine']==engine and s['Iteration']>=0) for engine in ('Baseline','Candidate')}
        if counts['Candidate'] < counts['Baseline']:
            losses.append(f"{row['Kind']} on {row['Domain'].upper()}: {counts['Baseline']:g} to {counts['Candidate']:g} correct requests out of {12*row['Requests']} across twelve measured calls.")
    projected.update(title=('Historical ' if historical else '')+'Shared linear run storage and scan costs',
        anchor='linear-run-workspace', evidence='linear-run-workspace-proof.json', evidence_label='Shared linear run proof',
        contracts='All 133,203 actual primitive run cases preserve widths and nested/repeated ownership. All 22,464 public module decisions match the preceding qualified binary across hints, transforms, scaling, truncations and checksum policies. Seven shared-workspace regression contracts pass on each framework; the exact core and test assembly pass 90 native ARM64 checks. Private primitive and public module evidence do not establish image recognition or comparative speed.',
        exploration_note=f"The separate traced nine-image diagnostic records {completions['before']}/27 to {completions['after']}/27 correct rejections after priming, with mean managed allocation {means['before']/1048576:.2f} to {means['after']/1048576:.2f} MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.",
        completion_note='', completion_notes=losses)
    projected['history_note']='These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.'
    return projected
