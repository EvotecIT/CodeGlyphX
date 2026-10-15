"""Project QR component mask decisions and costs with exact artifact identities."""
import hashlib
import json
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def project_qr_component_mask(root, qualification):
    proof, _, historical = resolve_proof_owner(root, qualification, 'QrComponentMaskProof')
    if not proof:
        return None

    def read(name):
        return json.loads((root / ('qr-component-mask-' + name + '.json')).read_text(encoding='utf-8-sig'))

    if read('proof') != proof:
        raise ValueError('QR component mask downloadable proof differs from its qualification owner')
    for mode, flags in (('vector256', (True, True)), ('vector128', (False, True)), ('scalar', (False, False))):
        data = read('equivalence-' + mode)
        if (data != proof['Equivalence'][mode] or data['CoreSha256'] != proof['CoreSha256']
                or data['Cases'] != 57344 or data['PixelClassificationsCompared'] != 72810496
                or data['Mismatches'] != 0 or data['CancelledAndExpiredMasksStopBeforeWrites'] is not True or data['UnusedPooledTailPreserved'] is not True
                or (data['Vector256HardwareAccelerated'], data['Vector128HardwareAccelerated']) != flags):
            raise ValueError('QR component mask decision proof differs from its core or execution mode')
    native = read('native-arm64')
    if (native != proof['NativeArm64'] or native['Source'] != proof['Source']
            or native['InputFiles']['CodeGlyphX.dll'] != proof['CoreSha256']
            or native['Architecture'] != 'arm64'
            or native['DecisionProof'] != proof['Equivalence']['vector128']):
        raise ValueError('QR component mask native ARM64 proof differs from its qualified binary')
    metadata_note = read('native-metadata-note')
    original_path = root / metadata_note['OriginalMaskReceiptFile']
    original = json.loads(original_path.read_text(encoding='utf-8-sig'))
    if (metadata_note != proof['NativeMetadataNote'] or 'Runtime' in native
            or not native['SDKVersion'] or not native['SelectedFrameworkVersion']
            or native['FrameworkRollForward'] != 'Disable'
            or metadata_note['CurrentSDKVersion'] != native['SDKVersion']
            or metadata_note['CurrentSelectedFrameworkVersion'] != native['SelectedFrameworkVersion']
            or metadata_note['CurrentFrameworkRollForward'] != 'Disable'
            or hashlib.sha256(original_path.read_bytes()).hexdigest().upper() != metadata_note['OriginalMaskReceiptSha256']
            or original['Source'] != proof['Source'] or original['DecisionProof'] != native['DecisionProof']):
        raise ValueError('Native SDK/framework metadata or original receipt differs')
    for mode in ('vector256', 'vector128', 'scalar'):
        component = read('component-equivalence-' + mode)
        if (component != proof['ComponentEquivalence'][mode]
                or component['CandidateCoreSha256'] != proof['CoreSha256']
                or component['BaselineCoreSha256'] != proof['PreviousCoreSha256']
                or component['ImagePolarityThresholdCases'] != 288 or component['PixelClassificationsCompared'] != 12279996
                or component['AcceptedComponentsCompared'] != 133 or component['Mismatches'] != 0
                or component['CancelledAndExpiredTraversalStops'] is not True
                or component['UnboundedTraversalPreserved'] is not True):
            raise ValueError('QR component traversal differs from its binaries or independent reference')
    if proof['Contracts'] != {'net10': 5, 'net8': 5, 'net472': 0}:
        raise ValueError('QR component mask framework contracts differ')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('QR component mask independent review does not cover its source')
    if len(proof['Experiments']) != 1 or proof['Experiments'][0]['Label'] != 'component-mask':
        raise ValueError('Missing exploratory QR component mask pairs')
    experiment = proof['Experiments'][0]
    rows = experiment['Packets']
    identity_keys = ('Source', 'CoreSha256', 'HostSha256', 'PreviousSource', 'PreviousCoreSha256', 'PreviousHostSha256')
    if (any(experiment[k] != proof[k] for k in identity_keys)
            or len(rows) != 8 or {(r['Kind'], r['Domain']) for r in rows} !=
            {(kind, domain) for kind in ('qr', 'texture', 'whole', 'fabric') for domain in ('ccd0', 'ccd1')}
            or any(any(r[k] != proof[k] for k in identity_keys)
                   or r['Requests'] != (19 if r['Kind'] == 'whole' else 1) for r in rows)):
        raise ValueError('Exploratory QR component mask workload or binary changed')
    project_packet_costs(root, proof, rows)
    projected = project_context_costs(root, qualification, 'QrComponentMaskProof')
    losses = []
    for label, packets in (('Final', proof['Packets']), ('Exploratory', rows)):
        for row in packets:
            packet = json.loads((root / row['PacketFile']).read_text(encoding='utf-8-sig'))
            counts = {engine: sum(sample['Metrics']['CorrectImages'] for sample in packet['Samples']
                                  if sample['Engine'] == engine and sample['Iteration'] >= 0)
                      for engine in ('Baseline', 'Candidate')}
            if counts['Candidate'] < counts['Baseline']:
                losses.append(f"{label} {row['Kind']} on {row['Domain'].upper()}: "
                              f"{counts['Baseline']:g} → {counts['Candidate']:g} correct image requests "
                              f"out of {12 * row['Requests']} across twelve measured calls.")
    projected.update(title='Historical QR component mask and costs' if historical else 'QR component mask and costs',
                     anchor='qr-component-mask', evidence='qr-component-mask-proof.json',
                     evidence_label='QR component mask proof', contracts=proof['ContractNote'],
                     completion_note=' '.join(losses),
                     exploration_note='Eight exploratory pairs on this same production binary remain separate from the final table. They include QR, texture, whole-image and fabric workloads, with every completion loss retained. Native ARM64 results prove pixel decisions only; the timing tables describe the Windows host. ' + metadata_note['Correction'])
    return projected
