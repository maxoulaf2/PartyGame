### US-E04-03 — Lobby sur l'écran TV

**Statut :** À faire

**En tant que** public
**je veux** voir sur la TV le QR code pour rejoindre et la liste des joueurs qui arrivent
**afin de** savoir qui est prêt et donner envie aux retardataires de rejoindre

**Critères d'acceptation**
- Étant donné l'écran TV ouvert sur `/display/`, quand il se connecte, alors il s'annonce comme `Display` et affiche, à partir de la projection `Display` : le QR code et l'URL de US-E02-03, le nombre de joueurs et la liste de leurs pseudos.
- Étant donné un joueur qui rejoint, quand son inscription est acceptée, alors son pseudo apparaît sur la TV en moins d'une seconde, sans rechargement.
- Étant donné un joueur renommé par le GM, quand le renommage est accepté, alors la TV affiche le nouveau pseudo.
- Étant donné un joueur dont le téléphone est déconnecté, quand la TV l'affiche, alors son pseudo reste dans la liste, atténué et accompagné d'une icône, jamais distingué par la couleur seule.
- Étant donné 20 joueurs aux pseudos de 16 caractères, quand la TV les affiche en 1080p, alors tous sont lisibles à 3 m, sans défilement, et rien d'essentiel ne se trouve à moins de 5 % d'un bord.
- Étant donné des pseudos comportant des émojis ou des caractères spéciaux (`<script>`, `&`), quand la TV les affiche, alors ils apparaissent tels quels, sans être interprétés comme du HTML.
- Étant donné la projection `Display`, quand les tests de non-fuite s'exécutent, alors elle ne contient ni jeton, ni code GM, ni liste des interfaces réseau.
- Étant donné un test E2E Playwright, quand trois joueurs rejoignent, alors leurs pseudos apparaissent sur la TV.

**Comportement en cas d'erreur**
Public : l'écran n'est jamais vide. Serveur injoignable, il garde le dernier affichage et l'indicateur discret de US-E05-02 apparaît après 3 s ; avant le premier snapshot, il affiche l'écran d'attente neutre. Adresse inconnue : le message neutre de US-E02-03 remplace le QR code, la liste des joueurs reste affichée.

**Notes techniques**
- L'adresse annoncée rejoint la projection `Display`, comme prévu en US-E02-02 : elle fait partie de l'état de la partie, initialisée au démarrage par `AddressSelection`, pour que son changement (US-E04-06) soit diffusé comme le reste. L'endpoint `/api/join` et le polling de `joinInfo.ts` sont retirés, avec leurs tests adaptés.
- La liste suit l'ordre d'arrivée, pour que les pseudos ne sautent pas à chaque inscription.
- La mise en page s'adapte au nombre de joueurs (colonnes, taille de police) ; aucun élément n'est coupé.
- Svelte échappe le texte par défaut : ne jamais utiliser `{@html}` pour un pseudo.
- Animation d'arrivée d'un joueur : facultative et sobre ; l'habillage est l'objet de E20.

**Hors périmètre**
- Bouton « Démarrer » de déblocage audio (E14).
- Jingles, thème et animations travaillées (E20).
- Plus de 20 joueurs : la mise en page reste correcte mais n'est pas optimisée.
