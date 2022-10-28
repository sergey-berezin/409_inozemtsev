# 409_inozemtsev
____________________________________________РАБОТА 2____________________________________________
Необходимо устновить arcfaceresnet100-8.onnx в директорию ArcFace_WPF_App\WpfApp1\bin\Debug\net6.0-windows
https://github.com/onnx/models/blob/main/vision/body_analysis/arcface/model/arcfaceresnet100-8.onnx

- Команда для установки пакета через диспетчер пакетов Visual Studio:
```
NuGet\Install-Package Kintobor_ArcFace_NuGet_Locks -Version 1.0.3
```

===================================================РАБОТА 1===================================================
Необходимо устновить arcfaceresnet100-8.onnx в директорию ArcFace_Test
https://github.com/onnx/models/blob/main/vision/body_analysis/arcface/model/arcfaceresnet100-8.onnx

NuGet пакет выполнен в двух версиях: с использованием оператора lock() и семафоров SemaphoreSlim.

- Команда для установки пакета с lock():
```
dotnet add package Kintobor_ARcFace_NuGet_Locks --version 1.0.1
```

- Команда для установки пакета с SemaphoreSlim:
```
dotnet add package Kintobor_ArcFace_NuGet_Semaphores --version 1.0.3
```
