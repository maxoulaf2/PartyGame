/**
 * The .NET server the E2E tests start behind the preview server, on a port of its own so that a
 * server left running on the default port does not get in the way.
 */
export const gameServerPort = 5199;

/** Fixed by `GameMaster:Code`: tests type it like a game master reading the server console. */
export const gameMasterCode = '246810';
