"""Project the DataBar-specific results through shared decoder qualification."""
from comparison_decoder_followups import load_decoder_followup


def project_databar_thresholds(root, qualification):
    loaded = load_decoder_followup(root, qualification, 'DataBarThresholdMasks',
        {'databar-positive':12,'databar-negative':31,'fabric':1}, 'Gs1DataBarOmnidirectional',
        {'net10.0':6,'net8.0':6,'net472':6}, {'net10.0':2754,'net8.0':2732,'net472':1177}, 199)
    if loaded is None:
        return None
    checked, proof, corpora, manifests, costs = loaded
    packets = proof['Packets']
    improvements, completion = [], []
    for row in packets:
        packet = checked({'File':row['PacketFile'],'Sha256':row['PacketSha256']})
        outcomes = checked({'File':row['OutcomesFile'],'Sha256':row['OutcomesSha256']})
        expected_order = [(r['Id'],r['Format'],r['Expected']) for r in manifests[row['Kind']]['Samples']]
        if any([(r['Id'],r['Format'],r['Expected']) for r in call['Outcomes']] != expected_order for call in outcomes['Calls']):
            raise ValueError('DataBar follow-up measured order differs from its input')
        lanes = {r['Engine']:r for r in packet['Summary']}
        if row['Kind']=='databar-positive':
            if any(r['Metrics']['CorrectImages'] != 10 for r in packet['Samples'] if r['Iteration']>=0):
                raise ValueError('DataBar follow-up positive outcomes changed')
            improvements.append(100*(1-lanes['Candidate']['MedianMs']/lanes['Baseline']['MedianMs']))
        if row['Kind']=='fabric':
            totals = {engine:sum(r['Metrics']['CorrectImages'] for r in packet['Samples'] if r['Engine']==engine and r['Iteration']>=0) for engine in lanes}
            completion.append(f"{row['Domain'].upper()}: {totals['Baseline']:g}/12 to {totals['Candidate']:g}/12 completed rejections")
    def counts(rows):
        rows = [r for r in rows if r['Engine']=='CodeGlyphX']
        return (sum(bool(r['Expected']) and r['Status']=='Correct' for r in rows),sum(not r['Expected'] and r['Status']=='Correct' for r in rows),sum(bool(r['Expected']) and bool(r['Unexpected']) for r in rows))
    before, current = counts(corpora['main-before']), counts(corpora['main-current'])
    neg_before, neg_current = counts(corpora['negative-before']), counts(corpora['negative-current'])
    return {'historical':qualification.get('LatestDecoderFollowup', 'DataBarThresholdMasks') != 'DataBarThresholdMasks' or qualification['MeasurementSource'] not in (proof['PreviousSource'],proof['Source']),'source':proof['Source'],'title':'DataBar recovery threshold follow-up',
        'anchor':'databar-thresholds','evidence':'databar-threshold-proof.json','evidence_label':'Decoder follow-up proof',
        'summary':f'The twelve-image DataBar positive batch takes {min(improvements):.1f}–{max(improvements):.1f}% less median time in this paired run. Every measured call retains 10/12 decoded images. The default negative-image workload is slower on both CPU domains and completes fewer times.',
        'completion_note':'; '.join(completion)+f'. The separate frozen corpus rerun retains {before[0]}/267 → {current[0]}/267 correct positives and {before[2]} → {current[2]} unexpected positive-image outputs. Main negative rejections are {before[1]}/12 → {current[1]}/12; supplementary rejections are {neg_before[1]}/25 → {neg_current[1]}/25. These conflicting completion changes do not establish a reliability gain.',
        'changes':proof['Changes'],'scope':proof['Scope'],'limits':proof['Limits'],'history_note':proof['HistoryNote'],
        'contracts':'All 6,663 distinct framework tests pass, including eighteen fixture-dependent reruns after correcting scratch fixture lookup. Six threshold contracts pass per framework; the exact core and test assembly also pass 199 native ARM64 functional checks and twenty additional Code128 pixel-API calls.',
        'costs':costs}
