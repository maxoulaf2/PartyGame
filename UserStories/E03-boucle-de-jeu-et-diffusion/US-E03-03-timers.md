### US-E03-03 — Timers déposés dans la file

**Statut :** Prête

**Résultat attendu**
Le moteur peut demander un timer par un effet. À échéance, le timer ne modifie rien lui-même : il dépose une entrée `TimerElapsed` dans la file, traitée par la boucle comme n'importe quelle autre entrée. Les comptes à rebours (E08) et la fenêtre d'arbitrage du buzzer (E13) reposeront sur ce mécanisme.

**Critères d'acceptation**
- Étant donné une transition qui émet `ScheduleTimer(timerId, dueAt)`, quand l'heure du `TimeProvider` atteint `dueAt`, alors une entrée `TimerElapsed(timerId)` est déposée dans la file.
- Étant donné un timer programmé, quand une transition émet `CancelTimer(timerId)` avant l'échéance, alors aucune entrée `TimerElapsed` n'est déposée pour lui.
- Étant donné un `ScheduleTimer` avec un identifiant déjà programmé, quand il est exécuté, alors il remplace le timer précédent.
- Étant donné un `TimerElapsed` qui arrive alors que l'état ne l'attend plus (timer annulé trop tard, phase changée), quand le moteur le traite, alors il le rejette comme n'importe quelle entrée obsolète : même instance d'état, aucun effet.
- Étant donné une échéance déjà passée au moment de la programmation, quand l'effet est exécuté, alors l'entrée est déposée immédiatement.
- Étant donné des tests avec `FakeTimeProvider`, quand on avance l'horloge, alors les entrées attendues sont déposées, sans aucune attente réelle.
- Étant donné l'arrêt de l'application, quand il est demandé, alors les timers en cours sont libérés.

**Comportement en cas d'erreur**
Sans objet pour les utilisateurs. Un timer qui ne peut pas être programmé est journalisé en `Error` ; la boucle continue.

**Notes techniques**
- Les timers sont créés avec `TimeProvider.CreateTimer`, jamais avec `Task.Delay` ou `System.Threading.Timer` directement.
- L'échéance est une heure absolue (`DateTimeOffset`) calculée par le moteur à partir de `context.Now`, pour que l'état connaisse l'instant exact (affiché plus tard aux clients comme échéance serveur).
- Le service de timers vit dans `PartyGame.Server` ; le moteur ne connaît que les effets et l'entrée.
- La boucle (US-E03-02) exécute les effets par `IEffectExecutor` (`PartyGame.Server/Games`) : le service de timers remplace l'implémentation provisoire `UnsupportedEffectExecutor`, et dépose ses entrées par `IGameInputWriter.WriteAsync`.
- Le moteur n'a pas encore de phase qui utilise un timer : les tests passent par un état ou un moteur de test. La première utilisation réelle est le compte à rebours du quiz (E08).

**Hors périmètre**
- Pause des timers quand la partie est en pause (E19).
- Reprogrammation des timers après une reprise sur crash (E11).
