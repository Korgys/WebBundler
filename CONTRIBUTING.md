# Contributing to WebBundler

Merci de contribuer. Ce guide decrit le minimum a suivre pour proposer une modification propre et facile a relire.

## Pre-requisites

- .NET SDK `10.0.x`
- Un environnement Windows, Linux ou macOS
- Git

## Mise en route

```bash
git clone https://github.com/Korgys/WebBundler.git
cd WebBundler
dotnet restore WebBundler.sln
```

## Verifications locales

Avant d'ouvrir une PR, execute au minimum :

```bash
dotnet build WebBundler.sln --configuration Release
dotnet test WebBundler.sln --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```

Si tu ne travailles que sur une zone precise, tu peux cibler le projet de test correspondant dans `tests/`, mais la validation finale doit passer sur la solution complete.

## Conventions de code

- Le depot utilise `Nullable` active, les `implicit usings` et `LangVersion=latest`.
- Le style est verifie pendant le build.
- Garde les changements petits et cibles.
- Preserve le comportement deterministe du bundler.
- Evite d'introduire de nouvelles dependances sans necessite claire.

## Tests

- Ajoute ou mets a jour les tests quand tu modifies une logique metier, un parseur, une validation ou un comportement CLI.
- Prefere des tests unitaires precis dans le projet de test le plus proche du code modifie.
- Pour les changements de configuration, verifie aussi les cas d'erreur et les cas de compatibilite.

## Documentation

Mets a jour la documentation quand tu changes :

- le CLI
- le schema de configuration
- le comportement MSBuild
- les formats de sortie

Les fichiers utiles se trouvent surtout dans `docs/` et `schemas/`.

## Fichiers generes

Ne commit pas les artefacts de build ou de test :

- `bin/`
- `obj/`
- `TestResults/`
- `artifacts/`
- `coveragereport/`
- `publish/`

## Pull requests

Une bonne PR devrait contenir :

- un resume clair du changement
- le contexte du probleme ou de l'amelioration
- la liste des tests executes
- les impacts potentiels sur la compatibilite si applicable

Garde les PRs focalisees sur un seul sujet quand c'est possible.
