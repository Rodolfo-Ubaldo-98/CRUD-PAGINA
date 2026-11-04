Modelos requeridos por face-api.js
===================================

Copia desde la carpeta dist del ZIP de vladmandic/face-api los archivos de estos tres modelos en esta carpeta:

1. tiny_face_detector_model-weights_manifest.json y todos sus archivos .bin asociados.
2. face_landmark_68_model-weights_manifest.json y todos sus archivos .bin asociados.
3. face_recognition_model-weights_manifest.json y todos sus archivos .bin asociados.

La aplicación los carga desde /models. No cambies los nombres de los manifiestos ni la estructura indicada en cada JSON.

También copia el archivo dist/face-api.js en:

  Registro.Servicio/Scripts/face-api.js

La captura necesita ejecutarse en HTTPS o en localhost y requiere permiso del navegador para utilizar la cámara.
