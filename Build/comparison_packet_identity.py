"""Reject current comparison packets from another source, binary or CPU placement."""


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
