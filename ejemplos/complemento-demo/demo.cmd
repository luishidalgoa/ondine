@echo off
rem Complemento de ejemplo. No baja nada: escribe por la salida estandar lo mismo
rem que escribiria uno de verdad, para poder probar la pantalla sin depender de
rem una fuente externa ni de ninguna herramienta instalada.
rem
rem Una linea, un mensaje, JSON. Lo que no sea JSON valido lo ignora la app, asi
rem que este aviso de aqui abajo tambien sirve de prueba de eso.
echo [demo] arrancado, esto no es JSON y debe ignorarse
if /I "%~1"=="listar" goto listar
if /I "%~1"=="traer" goto traer
echo {"tipo":"error","mensaje":"No se que hacer con el comando '%~1'"}
exit /b 2

:listar
echo {"tipo":"elemento","id":"d1","titulo":"El gorro de la suerte + El cazamariposas","duracion":1330}
echo {"tipo":"elemento","id":"d2","titulo":"Cuidado con los estornudos","duracion":662}
echo {"tipo":"elemento","id":"d3","titulo":"La lanza de la consideracion","duracion":668}
echo {"tipo":"elemento","id":"d4","titulo":"Un video que no es de la serie","duracion":95}
echo {"tipo":"hecho","ficheros":[]}
exit /b 0

:traer
echo {"tipo":"progreso","avance":0.0,"texto":"Empezando"}
echo {"tipo":"progreso","avance":0.5,"texto":"A la mitad"}
echo {"tipo":"progreso","avance":1.0,"texto":"Terminando"}
echo {"tipo":"hecho","ficheros":[]}
exit /b 0
