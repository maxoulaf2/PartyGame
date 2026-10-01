import { defineConfig, devices } from '@playwright/test';

const port = 4173;

export default defineConfig({
    testDir: './e2e',
    fullyParallel: true,
    reporter: 'list',
    use: {
        baseURL: `http://localhost:${port}`,
    },
    projects: [
        // Player pages must work on both mobile targets.
        { name: 'ios-safari', use: { ...devices['iPhone 15'] }, testMatch: /player\.spec\.ts/ },
        { name: 'android-chrome', use: { ...devices['Pixel 7'] }, testMatch: /player\.spec\.ts/ },
        // The TV screen and the GM console run on desktop browsers.
        {
            name: 'desktop-chrome',
            use: { ...devices['Desktop Chrome'] },
            testIgnore: /player\.spec\.ts/,
        },
    ],
    // Tests run against the production build, which is what phones will load.
    webServer: {
        command: `npm run build && npm run preview -- --port ${port} --strictPort`,
        url: `http://localhost:${port}`,
        reuseExistingServer: false,
    },
});
