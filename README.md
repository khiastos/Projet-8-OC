# Projet 8 : Améliorez votre application avec des systèmes distribués

Ce projet consiste en l'implémentation d'une nouvelle fonctionnalité, améliorer les tests unitaires qui échouaient, l'amélioration des performances de l'application, la rédaction de documentation et la mise en place d'un pipeline d'intégration continue.
---
## Outils et technologies utilisés

- **Visual Studio 2022**
- **C# / ASP.NET Core**
- **Swagger**
- **Entity Framework**
- **xUnit**
- **GitHub Actions**

---
## Implémentation d'une nouvelle fonctionnalité 

Il fallait que l'application soit capable de proposer aux utilisateurs les 5 attractions les plus proches d'eux. Il a fallu donc modifier l'endpoint GetNearbyAttractions afin qu'il renvoie désormais les 5 attractions les plus proches de l'utilisateur. 

Pour cela, on récupère toutes les attractions, on calcule pour chacune la distance avec la position de l'utilisateur et les points de récompense associés, puis on les trie par distance et on garde seulement les 5 premières. Enfin, on renvoie un DTO structuré contenant le nom de l'attraction, ses coordonnées, la localisation de l'utilisateur, la distance en miles (arrondie) et les rewards points.

---
## Amélioration des tests qui échouaient

_- Pour HighVolumeGetRewards :_ Le test échouait car les récompenses étaient calculées utilisateurs par utilisateurs, on a donc utilisé Parallel.ForEach au sein de ce test unitaire pour que plusieurs utilisateurs soient traités en même temps. 

_- Pour NearAllAttractions :_ Le test échouait car CalculateRewards n’attribuait des récompenses qu’aux attractions « proches », en fonction du proximityBuffer. Sauf que le test attend que toutes les attractions donnent une récompense, même très éloignées. En configurant le proximityBuffer avec int.MaxValue, la condition devient vraie pour toutes les attractions, ce qui permet d’attribuer une récompense pour chacune et de faire passer le test.

_- Pour GetTripDeals :_ Le test échouait car CalculateRewards ne générait pas toutes les récompenses, ce qui faisait que parfois l'utilisateur n'avait pas de récompenses, vu que c'était uniquement les attractions les plus proches qui en avaient une. 

---
## Amélioration des performances de l'application

L'application n'était pas faite pour fonctionner avec autant d'utilisateurs, ce qui fait qu'elle était trop lente et les utilisateurs s'en plaignaient. Il faudrait qu'elle supporte facilement 100 000 utilisateurs.

Il a donc fallu optimiser 2 services : GpsUtil et RewardsCentral : 

- _Pour GpsUtil :_ on a parallélisé l'appel à TrackUserLocation(), permettant de traiter la localisation de plusieurs utilisateurs en même temps au lieu la traiter de manière séquentielle.
- _Pour RewardsCentral :_ on a ajouté un cache de distance via un ConcurrentDictionary afin d'éviter de recalculer inutilement les distances, et on a également parallélisé le calcul des récompenses, ce qui permet de traiter plusieurs utilisateurs simultanément.

---
## Mise en place d'un pipeline d'intégration continue

Ce pipeline a été mis en place grâce à GitHub Actions, afin de permettre de compiler, tester et construire les Dll qui sont téléchargeables sous la forme d’une archive Zip.

---
## Installation  

1. `git clone https://github.com/khiastos/Projet-8-OC.git`  
2. Ouvrir la solution dans Visual Studio  
