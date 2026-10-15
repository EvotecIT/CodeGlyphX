"""Locate a historical proof's qualified owner without changing its identity."""
import json
import re
from pathlib import Path


def resolve_proof_owner(root, qualification, key):
    proof = qualification.get(key) or qualification.get('Historical' + key)
    if not proof:
        return None, None, False
    owner = qualification
    historical = key not in qualification
    visited = set()
    for _ in range(32):
        if owner.get(key) == proof:
            if (proof['Source'] != owner['MeasurementSource']
                    or proof['CoreSha256'] != owner['CandidateCoreSha256']):
                raise ValueError('Historical proof differs from its qualified source or core')
            return proof, owner, historical
        if owner.get('Historical' + key) != proof:
            raise ValueError('Historical proof differs along its qualification chain')
        name = owner.get('PriorQualificationPacket', 'previous-qualification.json')
        if (not isinstance(name, str) or re.fullmatch(r'[A-Za-z0-9_.-]+\.json', name) is None
                or Path(name).name != name or name in visited):
            raise ValueError('Invalid or cyclic historical qualification reference')
        visited.add(name)
        prior = json.loads((root / name).read_text(encoding='utf-8-sig'))
        identity = owner['NegativeRotationSources']['previous']
        if (owner['PreviousMeasurementSource'] != prior['MeasurementSource']
                or identity['Source'] != prior['MeasurementSource']
                or identity['CoreSha256'] != prior['CandidateCoreSha256']):
            raise ValueError('Historical qualification source or core link differs')
        owner = prior
    raise ValueError('Historical qualification chain exceeds its bounded depth')
