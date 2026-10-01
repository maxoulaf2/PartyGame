import type { Connect, Plugin } from 'vite';

/**
 * Gives the dev and preview servers the canonical page URLs of the .NET server (see FrontEndExtensions):
 * `/display` or `/Display/` is redirected to `/display/`. Without it, Vite answers these with the player page.
 * The redirect is temporary so that no browser remembers it if the routes change.
 */
export function canonicalPages(folders: readonly string[]): Plugin {
    const redirect: Connect.NextHandleFunction = (request, response, next) => {
        if (request.method !== 'GET' && request.method !== 'HEAD') {
            next();
            return;
        }
        const url = new URL(request.url ?? '/', 'http://localhost');
        const path = url.pathname.toLowerCase();
        const page = folders
            .map((folder) => `/${folder}/`)
            .find((canonical) => path === canonical || path === canonical.slice(0, -1));
        if (page === undefined || url.pathname === page) {
            next();
            return;
        }
        response.statusCode = 302;
        response.setHeader('Location', page + url.search);
        response.end();
    };

    return {
        name: 'partygame-canonical-pages',
        // Registered directly, so before Vite's own HTML fallback.
        configureServer: (server) => {
            server.middlewares.use(redirect);
        },
        configurePreviewServer: (server) => {
            server.middlewares.use(redirect);
        },
    };
}
