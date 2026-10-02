### US-E08-02 — Présentation de la question

**Statut :** À faire

**En tant que** public
**je veux** voir la question et ses propositions avant l'ouverture des réponses
**afin que** chacun ait le temps de lire avant que le compte à rebours ne démarre

**Critères d'acceptation**
- Étant donné une manche de quiz qui démarre, quand le snapshot est diffusé, alors sa première question est en phase `Presentation`.
- Étant donné une question en présentation, quand la TV l'affiche, alors elle montre le titre de la manche, le numéro de la question sur le total (« Question 3/5 »), son texte, son image s'il y en a une, et ses propositions, chacune avec sa lettre, sa forme et sa couleur.
- Étant donné la TV en 1080p, quand une question de 200 caractères a quatre propositions de 80 caractères, alors tout est lisible à 3 m, sans défilement ni texte coupé, et rien d'essentiel ne se trouve à moins de 5 % d'un bord.
- Étant donné un téléphone de joueur, quand la question est en présentation, alors il affiche le numéro de la question, son texte et les propositions (lettre, forme, couleur et texte), désactivées, avec l'indication que les réponses vont s'ouvrir.
- Étant donné la console GM, quand la question est en présentation, alors elle affiche la question, les propositions avec la bonne réponse signalée par une icône et un libellé (jamais par la couleur seule), et les boutons « Ouvrir les réponses » et « Passer la question » (US-E08-05).
- Étant donné une manche avec `shuffleChoices`, quand une question est présentée, alors ses propositions sont mélangées, dans le même ordre sur tous les écrans. Le mélange est reproductible en test avec une graine fixée.
- Étant donné un joueur inscrit pendant la présentation, quand les réponses s'ouvriront, alors il participera à la question (décision 5 du README).
- Étant donné les projections `Display` et `Player` en phase `Presentation`, quand les tests de non-fuite s'exécutent (US-E07-03), alors elles sont identiques quelle que soit la bonne réponse.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent le démarrage de la manche, le mélange reproductible et les projections des trois rôles. Un test E2E vérifie la question sur la TV, sur un téléphone et sur la console GM, avec la bonne réponse visible seulement sur cette dernière.

**Comportement en cas d'erreur**
Public : une image qui ne se charge pas laisse la question affichée sans elle (US-E06-04). Joueurs : connexion perdue, le téléphone garde la question affichée et verrouillée (US-E05-02). GM : idem.

**Notes techniques**
- Les propositions sont identifiées dans les projections et les intentions par leur position affichée (A à D), jamais par leur position dans le descripteur : sinon, l'ordre d'origine, souvent « bonne réponse en premier », se lirait à travers le mélange.
- Formes associées aux lettres, par exemple : A triangle, B losange, C cercle, D carré. Couleurs et formes sont des variables du thème (`shared/theme.css`).
- L'image passe par l'URL opaque de US-E06-04, présente dans la projection `Display` seulement.
- Intention GM `OpenAnswers(round, question)`, livrée avec US-E08-03.

**Hors périmètre**
- L'ouverture des réponses et le compte à rebours (US-E08-03).
- L'écran d'introduction de la manche (E18).
- Les animations d'apparition (E20).
