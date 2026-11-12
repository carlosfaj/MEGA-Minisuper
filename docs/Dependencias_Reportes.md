# Dependencias de exportación

Se fijan y conservan en `packages.lock.json`:

- PDFsharp 6.2.4, distribución Core y licencia MIT de esa versión: [fuente de licencia](https://raw.githubusercontent.com/empira/PDFsharp/v6.2.4/LICENSE).
- ClosedXML 0.105.0, licencia MIT y dependencias Open XML fijadas: [fuente](https://github.com/ClosedXML/ClosedXML/tree/0.105.0).
- System.Security.Cryptography.Pkcs 10.0.14 como dependencia explícita para evitar resolver una versión transitiva afectada por los avisos de NuGet. No se suprime el análisis de vulnerabilidades. Se consultó el [aviso de Microsoft](https://github.com/advisories/GHSA-846f-f764-76mx) y la revisión final no reportó paquetes vulnerables con las fuentes actuales.

La fuente estática Noto Sans Regular procede del [repositorio Noto](https://github.com/notofonts/noto-fonts/blob/main/hinted/ttf/NotoSans/NotoSans-Regular.ttf), licencia SIL Open Font License 1.1. SHA-256: `b85c38ecea8a7cfb39c24e395a4007474fa5a4fc864f6ee33309eb4948d232d5`. Se incluye como recurso del programa para que el PDF no dependa de instalar fuentes ni de Internet.

Los avisos de licencia se conservan en `docs/Licencias` y se copian a la distribución. La inicialización de fuentes PDF utiliza un resolvedor propio; no modifica las fuentes instaladas ni la configuración de Windows. Las API de exportación se basan en la [documentación oficial de PDFsharp](https://docs.pdfsharp.net/PDFsharp/Topics/Fonts/Font-Resolving.html) y [ClosedXML](https://docs.closedxml.io/en/latest/).

En 0.18.0 se incorporan MailKit/MimeKit.Core 5.0.0 (MIT) y System.Security.Cryptography.ProtectedData 10.0.14. Sus avisos se conservan con las licencias y los archivos de bloqueo fijan las dependencias. La revisión de vulnerabilidades de esa versión no reportó paquetes afectados con las fuentes actuales.
