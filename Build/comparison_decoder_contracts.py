"""Shared identity, profile and workload contracts for decoder comparison evidence."""
import math


def validate_native_contracts(proof, native, checks):
    if (native != proof['NativeArm64'] or native['Source'] != proof['Source'] or native['CoreSha256'] != proof['CoreSha256']
            or native['InputFiles']['CodeGlyphX.dll'] != proof['CoreSha256']
            or native['InputFiles']['CodeGlyphX.Tests.dll'] != proof['TestAssemblySha256']
            or native['Architecture'] != 'arm64' or native['FrameworkRollForward'] != 'Disable'
            or not native['SDKVersion'] or not native['SelectedFrameworkVersion']
            or native['DecisionProof']['Status'] != 'passed' or native['DecisionProof']['Checks'] != checks
            or len(native['DecisionProof']['Cases']) != checks or any(r['Status'] != 'passed' for r in native['DecisionProof']['Cases'])
            or native['DecisionProof']['CoreSha256'] != proof['CoreSha256']
            or native['DecisionProof']['TestAssemblySha256'] != proof['TestAssemblySha256']
            or native['DecisionProof']['CoreInformationalVersion'].split('+')[-1] != proof['Source']):
        raise ValueError('Workspace native proof differs from its exact core or tests')


def validate_profile_diagnostics(proof, owner, frozen, negatives, checked, read):
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
    return frozen_samples, means, completions


def validate_expanded_packets(proof, frozen, frozen_samples, checked):
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
    return supplemental
