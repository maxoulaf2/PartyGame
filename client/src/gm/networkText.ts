import type { ConnectionQuality, NetworkDiagnostic } from '../shared/contracts';
import { countText } from '../shared/i18n/countText';
import { fill } from '../shared/i18n/fill';
import { fr } from '../shared/i18n/fr';
import { formatNumber } from '../shared/i18n/numberText';
import { formatTime } from '../shared/i18n/timeText';
import { connectionWarnings } from '../shared/networkQuality';

const texts = fr.gm.network;

/** A diagnostic as the console lists it: the device, when, the verdict and the round trip. */
export function diagnosticText(diagnostic: NetworkDiagnostic): string {
    const summary = fill(texts.diagnostic, {
        device: texts.devices[diagnostic.device],
        time: formatTime(diagnostic.reportedAt),
        verdict: texts.verdicts[diagnostic.verdict],
    });
    return diagnostic.roundTripMedian === null
        ? summary
        : `${summary} · ${fill(texts.roundTrip, { value: formatNumber(diagnostic.roundTripMedian) })}`;
}

/** One measure of a connection, marked when poor. */
export interface QualityPart {
    readonly text: string;
    readonly poor: boolean;
}

/** The measures of a connection, in the order the console shows them. */
export function qualityParts(quality: ConnectionQuality): QualityPart[] {
    const warnings = connectionWarnings(quality);
    return [
        {
            text:
                quality.roundTrip === null
                    ? texts.noRoundTrip
                    : fill(texts.roundTrip, { value: formatNumber(quality.roundTrip) }),
            poor: warnings.roundTrip,
        },
        { text: texts.transports[quality.transport], poor: warnings.transport },
        {
            text: countText(texts.reconnections, quality.reconnections),
            poor: warnings.reconnections,
        },
    ];
}

/**
 * The address of the diagnostic page, at the address advertised to phones. The port is the one
 * the console was loaded from, as for the QR code of the TV screen.
 */
export function diagnosticUrl(joinUrl: string): string {
    return `${joinUrl}diagnostic/`;
}
