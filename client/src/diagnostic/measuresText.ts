import { countText } from '../shared/i18n/countText';
import { fill } from '../shared/i18n/fill';
import { fr } from '../shared/i18n/fr';
import { formatNumber } from '../shared/i18n/numberText';
import type { DiagnosticMeasures } from '../shared/networkQuality';

const texts = fr.diagnostic;

// One decimal: a slow network is often below 10 Mbit/s.
const throughputFormat = new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 1 });

/** A measure as the page lists it. */
export interface MeasureLine {
    readonly label: string;
    readonly value: string;
}

/** The measures of a test, in French, in the order the page lists them. */
export function describeMeasures(measures: DiagnosticMeasures): MeasureLine[] {
    const { transport, roundTrips, sameSubnet, throughput } = measures;
    if (transport === null) {
        return [{ label: texts.transport, value: texts.unreachable }];
    }
    return [
        { label: texts.transport, value: texts.transports[transport] },
        {
            label: texts.roundTrip,
            value:
                roundTrips === null
                    ? texts.notMeasured
                    : fill(texts.roundTripValue, {
                          median: formatNumber(roundTrips.median),
                          max: formatNumber(roundTrips.max),
                          jitter: formatNumber(roundTrips.jitter),
                      }),
        },
        {
            label: texts.stability,
            value: fill(texts.stabilityValue, {
                lost: countText(texts.lost, measures.lost),
                pings: formatNumber(measures.pings),
                reconnections: countText(texts.reconnections, measures.reconnections),
            }),
        },
        {
            label: texts.throughput,
            value:
                throughput === null
                    ? texts.notMeasured
                    : fill(texts.throughputValue, { value: throughputFormat.format(throughput) }),
        },
        {
            label: texts.network,
            value:
                sameSubnet === null
                    ? texts.networks.unknown
                    : sameSubnet
                      ? texts.networks.same
                      : texts.networks.other,
        },
    ];
}
