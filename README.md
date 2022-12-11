# 409_inozemtsev
=================================РАБОТА 4=================================
- Порядок запуска распределенного приложения:
1. Запуск приложения-сервера: _Lab4_V1a\ArcFace_Distributed_App\Server_
2. Запуск приложения-клиента: _Lab4_V1a\ArcFace_Distributed_App\Client_
\
\
=================================РАБОТА 3=================================  
- Команда для установки пакета через диспетчер пакетов Visual Studio:
```
NuGet\Install-Package Kintobor_ArcFace_NuGet_Locks_With_Embeddings -Version 1.0.0
```
\
\
=================================РАБОТА 2=================================  
- Команда для установки пакета через диспетчер пакетов Visual Studio:
```
NuGet\Install-Package Kintobor_ArcFace_NuGet_Locks -Version 1.0.5
```
\
\
=================================РАБОТА 1=================================  
NuGet пакет выполнен в двух версиях: с использованием оператора lock() и семафоров SemaphoreSlim.

- Команда для установки пакета с lock():
```
dotnet add package Kintobor_ARcFace_NuGet_Locks --version 1.0.5
```

- Команда для установки пакета с SemaphoreSlim:
```
dotnet add package Kintobor_ArcFace_NuGet_Semaphores --version 1.0.4
```
