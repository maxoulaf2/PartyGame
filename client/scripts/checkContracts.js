// Fails when the TypeScript types in src/shared/contracts, or schemas/pack.schema.json, no longer match PartyGame.Contracts.
// `dotnet test` runs the same check and is the reference: here it is skipped when the .NET SDK is missing,
// so that the client stays checkable without it.
import { spawnSync } from 'node:child_process';

const probe = spawnSync('dotnet', ['--version'], { stdio: 'ignore' });
if (probe.error || probe.status !== 0) {
    console.warn(
        'warning: .NET SDK not found, generated contracts not verified (dotnet test checks them).',
    );
    process.exit(0);
}

const result = spawnSync(
    'dotnet',
    [
        'run',
        '--project',
        '../tools/PartyGame.TypeGen',
        '--verbosity',
        'quiet',
        '--',
        'src/shared/contracts',
        '--schema',
        '../schemas/pack.schema.json',
        '--verify',
    ],
    { stdio: 'inherit' },
);
process.exit(result.status ?? 1);
