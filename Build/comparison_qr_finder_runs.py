"""Project QR finder run results through shared decoder qualification."""
from comparison_decoder_followups import load_decoder_followup


def project_qr_finder_runs(root, qualification):
    loaded = load_decoder_followup(root, qualification, 'QrFinderRuns',
        {'qr-positive':37,'qr-negative':31,'fabric':1}, 'QrCode',
        {'net10.0':9,'net8.0':9,'net472':0}, {'net10.0':2763,'net8.0':2741,'net472':1177}, 208)
    if loaded is None:
        return None
    checked, proof, corpora, manifests, costs = loaded
    decisions = checked(proof['DetectorEquivalence'])
    before = checked(decisions['Baseline'])
    current = checked(decisions['Candidate'])
    if (decisions['Cases'] != 2400 or decisions['Mismatches'] or len(before['Rows']) != 2400
            or before['Rows'] != current['Rows'] or before['CoreSha256'] != proof['PreviousCoreSha256']
            or current['CoreSha256'] != proof['CoreSha256']):
        raise ValueError('QR finder decision equivalence differs')
    scalar = checked(proof['ScalarContracts'])
    scalar_decisions = checked(scalar['DetectorSnapshots'])
    if (scalar['Source'] != proof['Source'] or scalar['CoreSha256'] != proof['CoreSha256']
            or scalar['TestAssemblySha256'] != proof['TestAssemblySha256'] or scalar['Passed'] != 18
            or scalar['Failed'] or scalar['Skipped'] or not scalar['HardwareIntrinsicsDisabled']
            or scalar_decisions['Vector128HardwareAccelerated'] or scalar_decisions['Vector256HardwareAccelerated']
            or scalar_decisions['Rows'] != before['Rows'] or scalar_decisions['CoreSha256'] != proof['CoreSha256']):
        raise ValueError('QR finder scalar contracts are incomplete')
    if set(proof['CorpusReceipts']) != set(corpora):
        raise ValueError('QR finder corpus receipts are missing')
    for key, reference in proof['CorpusReceipts'].items():
        receipt = checked(reference)
        previous = key.endswith('-before')
        if (receipt['Source'] != proof['PreviousSource' if previous else 'Source']
                or receipt['CoreSha256'] != proof['PreviousCoreSha256' if previous else 'CoreSha256']
                or receipt['HostSha256'] != proof['PreviousHostSha256' if previous else 'HostSha256']
                or receipt['OutputSha256'] != proof['Corpus'][key]['OriginalOutputSha256']
                or receipt['ManifestSha256'] != proof['FrozenManifests'][key.split('-')[0]]['OriginalInputSha256']
                or receipt['FrameworkVersion'] != '10.0.13' or receipt['FrameworkRollForward'] != 'Disable'):
            raise ValueError('QR finder standalone corpus identity differs')
    reductions, completion = [], []
    for row in proof['Packets']:
        packet = checked({'File':row['PacketFile'],'Sha256':row['PacketSha256']})
        outcomes = checked({'File':row['OutcomesFile'],'Sha256':row['OutcomesSha256']})
        expected_order = [(r['Id'],r['Format'],r['Expected']) for r in manifests[row['Kind']]['Samples']]
        if any([(r['Id'],r['Format'],r['Expected']) for r in call['Outcomes']] != expected_order for call in outcomes['Calls']):
            raise ValueError('QR finder measured request order differs')
        lanes = {r['Engine']:r for r in packet['Summary']}
        if row['Kind'] == 'qr-negative':
            reductions.append(100*(1-lanes['Candidate']['MedianMs']/lanes['Baseline']['MedianMs']))
        if row['Kind'] == 'qr-positive' and any(r['Metrics']['CorrectImages'] != 34 for r in packet['Samples'] if r['Iteration'] >= 0):
            raise ValueError('QR finder positive recognition changed')
        if row['Kind'] == 'fabric':
            totals = {engine:sum(r['Metrics']['CorrectImages'] for r in packet['Samples'] if r['Engine']==engine and r['Iteration'] >= 0) for engine in lanes}
            completion.append(f"{row['Domain'].upper()}: {totals['Baseline']:g}/12 → {totals['Candidate']:g}/12 completed fabric rejections")
    def counts(rows):
        rows = [r for r in rows if r['Engine']=='CodeGlyphX']
        return (sum(bool(r['Expected']) and r['Status']=='Correct' for r in rows),
                sum(not r['Expected'] and r['Status']=='Correct' for r in rows),
                sum(bool(r['Expected']) and bool(r['Unexpected']) for r in rows))
    old, new = counts(corpora['main-before']), counts(corpora['main-current'])
    negative_old, negative_new = counts(corpora['negative-before']), counts(corpora['negative-current'])
    negative_change = (f'takes {min(reductions):.1f}–{max(reductions):.1f}% less median time' if min(reductions) >= 0
                       else f'has median-time changes of {-max(reductions):+.1f}% to {-min(reductions):+.1f}%')
    return {'historical':qualification.get('LatestDecoderFollowup') != 'QrFinderRuns','source':proof['Source'],
        'title':'QR finder run follow-up','anchor':'qr-finder-runs','evidence':'qr-finder-runs-proof.json','evidence_label':'QR finder run proof',
        'summary':f'The 31-image QR-only negative batch {negative_change}. Positive QR timing and default all-format negative completion are mixed; every result remains included.',
        'completion_note':'; '.join(completion)+f'. The frozen main rerun records {old[0]}/267 → {new[0]}/267 correct positives and {old[2]} → {new[2]} unexpected positive-image outputs. Main negative rejections: {old[1]}/12 → {new[1]}/12; supplementary rejections: {negative_old[1]}/25 → {negative_new[1]}/25. These runs do not establish a reliability gain.',
        'changes':proof['Changes'],'scope':proof['Scope'],'limits':proof['Limits'],'history_note':proof['HistoryNote'],
        'contracts':'All 6,681 distinct framework tests pass. Nine finder contracts pass on each modern framework; eighteen focused cases also pass with hardware intrinsics disabled. The exact managed core and test assembly pass 208 native ARM64 functional checks plus twenty additional Code128 pixel-API calls. All 2,400 prior/current detector snapshots match.',
        'exploration_note':proof['ExplorationNote'],'costs':costs}
