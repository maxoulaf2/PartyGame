import { defineConfig, devices } from '@playwright/test';
import {
    advertisedAddress,
    gameMasterCode,
    gameServerPort,
    packDirectory,
} from './e2e/gameServer.ts';

const port = 4173;
const mobilePages = /(player|gm|packs|reconnection|buildReload)\.spec\.ts/;
// Tests that change the shared server for every other test: starting the game, the address.
const serverWide = /(launch|address)\.spec\.ts/;

export default defineConfig({
    testDir: './e2e',
    fullyParallel: true,
    reporter: 'list',
    use: {
        baseURL: `http://localhost:${port}`,
    },
    projects: [
        // Player pages must work on both mobile targets, and so must the GM console, often driven
        // from a phone.
        { name: 'ios-safari', use: { ...devices['iPhone 15'] }, testMatch: mobilePages },
        { name: 'android-chrome', use: { ...devices['Pixel 7'] }, testMatch: mobilePages },
        // The TV screen and the GM console run on desktop browsers.
        {
            name: 'desktop-chrome',
            use: { ...devices['Desktop Chrome'] },
            testIgnore: [/(player|reconnection|buildReload)\.spec\.ts/, serverWide],
        },
        // Starting the game cannot be undone on the shared server, and a new address changes every
        // QR code: once every other test is done.
        {
            name: 'launch',
            use: { ...devices['Desktop Chrome'] },
            testMatch: serverWide,
            dependencies: ['ios-safari', 'android-chrome', 'desktop-chrome'],
        },
    ],
    webServer: [
        // Tests run against the production build, which is what phones will load.
        {
            command: `npm run build && npm run preview -- --port ${port} --strictPort`,
            url: `http://localhost:${port}`,
            reuseExistingServer: false,
            env: { PARTYGAME_SERVER_URL: `http://localhost:${gameServerPort}` },
        },
        // The real server behind the preview proxy, for the hub. Started once the build has
        // rewritten its web root.
        {
            command: `dotnet run --project src/PartyGame.Server -- --Network:Port=${gameServerPort} --GameMaster:Code=${gameMasterCode} --Network:AdvertisedAddress=${advertisedAddress} "--Packs:Directory=${packDirectory}"`,
            cwd: '..',
            url: `http://localhost:${gameServerPort}/health`,
            reuseExistingServer: false,
            timeout: 180_000,
        },
    ],
});
