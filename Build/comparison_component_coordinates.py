"""Project exact component coordinate contracts, kernel costs and complete scans."""
import hashlib
import json
import math
import statistics
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def checked_json(root, row):
    name = row['File']
    if '/' in name or '\\' in name:
        raise ValueError('Component proof file must stay in its evidence root')
    raw = (root / name).read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != row['Sha256']:
        raise ValueError('Component proof bytes differ')
    return json.loads(raw.decode('utf-8-sig'))


def project_kernel_cost(root, proof, row):
    packet = json.loads((root / row['PacketFile']).read_text(encoding='utf-8-sig'))
    outcomes = checked_json(root, {'File':row['OutcomesFile'], 'Sha256':row['OutcomesSha256']})
    domain = row['Domain']
    metadata = packet['Metadata']
    if (row['Variants'] != 16 or metadata['benchmark.ManifestSha256'] != row['ManifestSha256']
            or metadata['benchmark.CandidateCoreSha256'] != proof['CoreSha256']
            or metadata['benchmark.BaselineCoreSha256'] != proof['PreviousCoreSha256']
            or metadata['benchmark.CandidateSource'] != proof['Source'] or metadata['gitSha'] != proof['Source']
            or metadata['gitWorktreeClean'] != 'true' or metadata['processPriority'] != 'Normal'
            or metadata['processAffinityMask'] != {'ccd0':'0xFFFF','ccd1':'0xFFFF0000'}[domain]
            or metadata['outlierMode'] != 'None' or outcomes['RunId'] != packet['RunId']
            or len(packet['Samples']) != 24 or len(packet['Summary']) != 2):
        raise ValueError('Component kernel binary, placement, workload or sample count differs')
    calls = outcomes['Calls']
    if (len(calls) != 30 or {(c['Engine'],c['Iteration']) for c in calls} !=
            {(e,i) for e in ('Baseline','Candidate') for i in range(-3,12)}):
        raise ValueError('Incomplete component kernel priming or measured outcomes')
    reference = next(c['Outcomes'] for c in calls if c['Engine']=='Baseline' and c['Iteration']==0)
    if (len(reference) != 16 or [r['Variant'] for r in reference] != list(range(16))
            or any(r['Components'] < 0 or len(r['DescriptorSha256']) != 64 for r in reference)
            or sum(r['Components'] for r in reference) != 163
            or any(c['Outcomes'] != reference for c in calls)):
        raise ValueError('Actual component bounds, areas or order differ between calls')
    summaries = {r['Engine']:r for r in packet['Summary']}
    for engine in ('Baseline','Candidate'):
        samples = [r for r in packet['Samples'] if r['Engine']==engine]
        summary = summaries[engine]
        if (len(samples) != 12 or {r['Iteration'] for r in samples} != set(range(12))
                or any(r['Status'] != 0 or r['Metrics']['Variants'] != 16 or r['Metrics']['Components'] != 163 for r in samples)
                or summary['Status'] != 'Succeeded' or summary['FailureCount'] != 0 or summary['SampleCount'] != 12
                or summary['Metrics']['Variants'] != 16 or summary['Metrics']['Components'] != 163
                or not math.isclose(summary['MedianMs'],statistics.median(r['DurationMs'] for r in samples),abs_tol=1e-9)
                or not math.isclose(summary['Metrics']['AllocatedBytes'],sum(r['AllocatedBytes'] for r in samples)/12,abs_tol=1e-8)):
            raise ValueError('Component kernel samples differ from actual outcomes or summaries')
    before, current = (summaries[e] for e in ('Baseline','Candidate'))
    ratio = current['MedianMs']/before['MedianMs']
    return {'domain':domain.upper(),'variants':16,'components':163,
            'before_ms':f"{before['MedianMs']:.2f}",'current_ms':f"{current['MedianMs']:.2f}",
            'ratio':f'{ratio:.3f}','reduction':f'{100*(1-ratio):.1f}%',
            'before_mib':f"{before['Metrics']['AllocatedBytes']/1048576:.2f}",
            'current_mib':f"{current['Metrics']['AllocatedBytes']/1048576:.2f}"}


def project_component_coordinates(root, qualification):
    proof, owner, historical = resolve_proof_owner(root, qualification, 'ImageComponentCoordinateProof')
    if not proof:
        return None
    def read(name):
        return json.loads((root/('component-coordinates-'+name+'.json')).read_text(encoding='utf-8-sig'))
    if read('proof') != proof or proof['Contracts'] != {'net10':9,'net8':9,'net472':5}:
        raise ValueError('Component coordinate download or framework contracts differ')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('Component arithmetic review is not current')
    reference = checked_json(root, {'File':proof['ReferenceContractsFile'],'Sha256':proof['ReferenceContractsSha256']})
    if (reference['Source'] != proof['Source'] or reference['CoreSha256'] != proof['CoreSha256']
            or reference['TestAssemblySha256'] != proof['TestAssemblySha256']
            or reference['FrameworkCases'] != proof['Contracts'] or reference['CoordinateWidths'] != 1023
            or reference['BinaryGraphVariantsPerFullFramework'] != 128 or reference['QrGraphVariantsPerFullFramework'] != 128):
        raise ValueError('Independent coordinate and graph contracts differ')
    native = read('native-arm64')
    decision = native['DecisionProof']
    if (native != proof['NativeArm64'] or native['Source'] != proof['Source'] or native['CoreSha256'] != proof['CoreSha256']
            or native['Architecture'] != 'arm64' or not native['SDKVersion'] or not native['SelectedFrameworkVersion']
            or native['FrameworkRollForward'] != 'Disable' or decision['Architecture'] != 'Arm64'
            or decision['Status'] != 'passed' or decision['Checks'] != 41 or len(decision['Cases']) != 41
            or len({r['Case'] for r in decision['Cases']}) != 41 or any(r['Status'] != 'passed' for r in decision['Cases'])
            or decision['CoreSha256'] != proof['CoreSha256'] or decision['TestAssemblySha256'] != proof['TestAssemblySha256']
            or native['InputFiles']['CodeGlyphX.dll'].upper() != proof['CoreSha256']
            or native['InputFiles']['CodeGlyphX.Tests.dll'].upper() != proof['TestAssemblySha256']
            or native['InputFiles']['Fixtures/Qr/perspective-bookmark.png'].upper() != proof['NativeFixtureSha256']
            or decision['CoreInformationalVersion'].split('+')[-1] != proof['Source']):
        raise ValueError('Native component and public decoder proof differs from its exact binaries')
    diagnostic = read('profile-decision')
    if (diagnostic != proof['ProfileDiagnostic'] or diagnostic['Source'] != proof['PreviousSource']
            or diagnostic['CoreSha256'] != proof['PreviousCoreSha256'] or diagnostic['Images'] != 9
            or diagnostic['PrimingCalls'] != 9 or diagnostic['MeasuredDiagnosticCalls'] != 27):
        raise ValueError('Component investigation profile belongs to another baseline')
    if len(proof['ProfileFiles']) != 2:
        raise ValueError('Missing component profile evidence')
    diagnostic_rows, profile = (checked_json(root,row) for row in proof['ProfileFiles'])
    if (diagnostic_rows['CoreSha256'] != proof['PreviousCoreSha256'] or len(diagnostic_rows['Rows']) != 36
            or len({r['Id'] for r in diagnostic_rows['Rows']}) != 9
            or any(len(r['Outcomes']) != 1 for r in diagnostic_rows['Rows'])
            or profile['AllocationTicks'] < 1 or profile['ThreadTimeSamples'] < 1 or not profile['TopProductThreadTimeStacks']):
        raise ValueError('Incomplete per-image diagnostic profile')
    recipe = read('kernel-recipe')
    if (recipe != proof['KernelRecipe'] or recipe['Role'] != 'ActualPrivateComponentCollectionV1'
            or recipe['Method'] != 'QrPixelDecoder.FindComponents' or recipe['Images'] != 4 or recipe['Variants'] != 16
            or recipe['Thresholds'] != [96,160] or recipe['Polarities'] != [False,True] or recipe['BudgetMilliseconds'] != 0
            or recipe['Luminance'] != '(299*r + 587*g + 114*b + 500) / 1000'
            or hashlib.sha256(recipe['ClassSource'].encode('utf-8')).hexdigest().upper() != recipe['ClassSha256']
            or hashlib.sha256(recipe['SpecificationSource'].encode('utf-8')).hexdigest().upper() != recipe['SpecificationSha256']):
        raise ValueError('Actual component kernel recipe or scope changed')
    manifest_row = proof['KernelInputManifest']
    manifest = checked_json(root,manifest_row)
    expected_ids = {'multi-qrcode-1/1.png','falsepositives/03.png','falsepositives/15.png','falsepositives/19.png'}
    if len(manifest['Samples']) != 4 or {r['Id'] for r in manifest['Samples']} != expected_ids:
        raise ValueError('Component kernel image selection changed')
    frozen_row = proof['FrozenCorpusManifest']
    frozen = checked_json(root,frozen_row)
    frozen_by_id = {r['Id']:r for r in frozen['Samples']}
    if (frozen_row['OriginalInputSha256'] != owner['CorpusManifestSha256']
            or any(r != frozen_by_id.get(r['Id']) for r in manifest['Samples'])):
        raise ValueError('Component kernel image bytes or oracles differ from the frozen corpus')
    manifest_hash = manifest_row['OriginalInputSha256']
    if (len(proof['KernelPackets']) != 2 or {r['Domain'] for r in proof['KernelPackets']} != {'ccd0','ccd1'}
            or any(r['ManifestSha256'] != manifest_hash for r in proof['KernelPackets'])):
        raise ValueError('Missing component kernel domain or changed input manifest')
    for key, expected in (('SupplementalPackets',{(k,d) for k in ('databar-positive','databar-negative') for d in ('ccd0','ccd1')}),
                          ('ExploratoryPackets',{(k,d) for k in ('qr','negative15','texture','timeout9') for d in ('ccd0','ccd1')})):
        if len(proof[key]) != len(expected) or {(r['Kind'],r['Domain']) for r in proof[key]} != expected:
            raise ValueError('Missing retained public-scan workload')
        requests = {'databar-positive':12,'databar-negative':31,'qr':1,'negative15':1,'texture':1,'timeout9':9}
        if any(r['Requests'] != requests[r['Kind']] for r in proof[key]):
            raise ValueError('Retained public-scan request count changed')
    projected = project_context_costs(root,qualification,'ImageComponentCoordinateProof')
    projected['costs'] += project_packet_costs(root,proof,proof['SupplementalPackets'])
    projected['exploratory_costs'] = project_packet_costs(root,proof,proof['ExploratoryPackets'])
    projected['kernel_costs'] = [project_kernel_cost(root,proof,row) for row in proof['KernelPackets']]
    reductions = [float(r['reduction'].rstrip('%')) for r in projected['kernel_costs']]
    losses = []
    for label, rows in (('Final',proof['Packets']+proof['SupplementalPackets']),('Exploratory',proof['ExploratoryPackets'])):
        for row in rows:
            packet = json.loads((root/row['PacketFile']).read_text(encoding='utf-8-sig'))
            counts = {e:sum(s['Metrics']['CorrectImages'] for s in packet['Samples'] if s['Engine']==e and s['Iteration']>=0)
                      for e in ('Baseline','Candidate')}
            if counts['Candidate'] < counts['Baseline']:
                losses.append(f"{label} {row['Kind']} on {row['Domain'].upper()}: {counts['Baseline']:g} to {counts['Candidate']:g} correct requests out of {12*row['Requests']} across twelve measured calls.")
    projected.update(title=('Historical ' if historical else '')+'Image component coordinates and scan costs',
        anchor='component-coordinate-costs',evidence='component-coordinates-proof.json',evidence_label='Component coordinate proof',
        contracts=f"The isolated QR component traversal median falls by {min(reductions):.1f}% to {max(reductions):.1f}% across the two processor domains. Complete-scan costs and deadline outcomes remain separate below. Independent coordinate and graph references preserve pixel order, bounds and area, and the exact binary passes 41 native ARM64 functional checks.",
        exploration_note='Eight exploratory public-scan pairs retain every priming and measured per-image result in a separate table, including all slower passes and incomplete searches.',
        completion_note='',completion_notes=losses,kernel_note=recipe['Scope'])
    projected['history_note']='Each public-scan table retains one decoder session per binary. Earlier fresh-context costs remain separately identified in the historical context inventory.'
    return projected
