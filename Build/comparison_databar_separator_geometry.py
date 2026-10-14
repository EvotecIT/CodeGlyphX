"""Project tilted stacked DataBar separator recognition and complete scan costs."""
from comparison_photograph_recovery import project_photograph_recovery


def project_databar_separator_geometry(root, qualification):
    return project_photograph_recovery(root, qualification,
        'DataBarSeparatorGeometryProof', 'databar-separator-', 22, 147, (22,15,7),
        'Stacked DataBar separator geometry and complete scan costs',
        'Twenty-two separator geometry cases pass on each framework. Fifteen of these 22 cases fail against the exact preceding core. Independent stacked photographs pass in all four rotations and both polarities; synthetic simple and omnidirectional layouts preserve their payloads and reject the wrong layout and missing separators. The exact core and test assembly pass 147 native ARM64 checks, plus 20 Code128 pixel-API calls. The separate 52-photo development set is excluded from the headline 267-image ranking.')
