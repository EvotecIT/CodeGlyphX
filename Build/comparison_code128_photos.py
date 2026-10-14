"""Project the original Code128 recovery proof with its own source identity."""
from comparison_photograph_recovery import project_photograph_recovery


def project_code128_photos(root, qualification):
    projected = project_photograph_recovery(root, qualification,
        'Code128PhotoRecoveryProof', 'code128-photo-', 20, 125, (20,15,5),
        'Code128 photo recovery and complete scan costs',
        'Twenty photograph and row-recovery regression cases pass on each framework. Fifteen of these 20 cases fail against the exact preceding core. Public checks cover transformed images, three adjacent matching rows, checksum rejection and reader-initialization identity. The exact core and test assembly pass 125 native ARM64 checks. The separate 52-photo development set is reported above and is excluded from the headline 267-image ranking.')
    if projected:
        projected["evidence_label"] = "Code128 photo recovery proof"
    return projected
