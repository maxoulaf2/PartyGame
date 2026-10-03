import type { Incident, IncidentCode, Role } from '../shared/contracts';
import { fill, roundText } from '../shared/i18n/fill';
import { formatNumber } from '../shared/i18n/numberText';
import { fr } from '../shared/i18n/fr';
import { formatTime } from '../shared/i18n/timeText';

// Typed by the generated codes: a code added on the server without its message fails the check.
const codeTexts: Readonly<Record<IncidentCode, string>> = fr.gm.incidents.codes;

const roleTexts: Readonly<Record<Role, string>> = fr.gm.incidents.roles;

/** What went wrong, in French, for the game master. */
export function describeIncident(incident: Incident): string {
    return fill(codeTexts[incident.code], {
        role: incident.role === null ? '' : roleTexts[incident.role],
    });
}

/** The round in progress when the incident happened, if any. */
export function incidentRoundText(incident: Incident): string {
    const round = incident.round;
    return round === null
        ? fr.gm.incidents.outsideRound
        : fill(roundText(fr.gm.incidents.round, round), { title: round.title });
}

/** When the incident last happened, and how many times when it repeated. */
export function incidentTimeText(incident: Incident): string {
    const time = formatTime(incident.lastOccurredAt);
    return incident.count > 1
        ? fill(fr.gm.incidents.repeated, { count: formatNumber(incident.count), time })
        : fill(fr.gm.incidents.at, { time });
}
