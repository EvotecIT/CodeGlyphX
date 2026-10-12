"""Reject current comparison packets from another source, binary or CPU placement."""


def validate_paired_packet(packet, source, core, previous_core, domain, requests, manifest_hash):
    metadata = packet['Metadata']
    if (metadata['benchmark.CandidateSource'] != source
            or metadata['benchmark.CandidateCoreSha256'] != core
            or metadata['benchmark.BaselineCoreSha256'] != previous_core
            or metadata['benchmark.CandidateWorktreeClean'] != 'true'
            or metadata['benchmark.ManifestSha256'] != manifest_hash
            or metadata['processAffinityMask'] != ('0xFFFF' if domain == 'ccd0' else '0xFFFF0000')
            or metadata['processPriority'] != 'Normal' or metadata['outlierMode'] != 'None'
            or len(packet['Summary']) != 2
            or {r['Engine'] for r in packet['Summary']} != {'Baseline', 'Candidate'}
            or any(r['Status'] != 'Succeeded' or r['FailureCount'] or r['SampleCount'] != 12 for r in packet['Summary'])):
        raise ValueError('Paired packet source, binary, inputs, placement or samples differ')
    for engine in ('Baseline','Candidate'):
        samples = [r for r in packet['Samples'] if r['Engine'] == engine and r['Iteration'] >= 0]
        if len(samples) != 12 or any(r['Metrics']['Requests'] != requests for r in samples):
            raise ValueError('Paired packet request samples differ')


def validate_qualified_whole_image_packet(packet, qualification, domain):
    paired = qualification['PairedDecodeCost']
    previous = qualification['NegativeRotationSources']['previous']
    if paired['Source'] != qualification['MeasurementSource'] or paired['PreviousSource'] != previous['Source']:
        raise ValueError('Paired whole-image sources differ from the qualified binaries')
    validate_paired_packet(packet, paired['Source'], qualification['CandidateCoreSha256'], previous['CoreSha256'],
                          domain, paired['Requests'], qualification['LatencyManifestSha256'])


def validate_current_packet(packet, qualification, kind, domain):
    metadata = packet['Metadata']
    expected_count = 24 if kind == 'generation' else 3
    if (metadata['gitSha'] != qualification['MeasurementSource']
            or metadata['gitWorktreeClean'] != 'true'
            or metadata['benchmark.CoreAssemblySha256'] != qualification['CandidateCoreSha256']
            or metadata['processAffinityMask'] != ('0xFFFF' if domain == 'ccd0' else '0xFFFF0000')
            or metadata['processPriority'] != 'Normal'
            or metadata['outlierMode'] != 'None'
            or len(packet['Summary']) != expected_count
            or any(row['Status'] != 'Succeeded' or row['FailureCount'] or row['SampleCount'] != 12
                   for row in packet['Summary'])):
        raise ValueError(f'Unqualified current {kind} packet for {domain}')
    if kind == 'generation':
        if metadata['benchmark.ComparisonAssemblySha256'] != qualification['ComparisonAssemblySha256']:
            raise ValueError('Generation host differs from the qualified comparison host')
    elif metadata['benchmark.ManifestSha256'] != qualification['LatencyManifestSha256']:
        raise ValueError('Decode requests differ from the qualified latency manifest')
