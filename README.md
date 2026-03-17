# TraceFit

Aplicativo em .NET MAUI com proposta de desafio visual e motor, onde o usuario desenha formas e recebe pontuacao em tempo real.

## Recursos identificados

- Area de desenho em tela cheia
- Medidor visual de desempenho
- Regras de avaliacao do tracado
- Mensagens motivacionais durante a experiencia

## Estrutura principal

- `Core/TraceEngine.cs`: regras de avaliacao e pontuacao
- `Core/BoardDrawable.cs` e `Core/GaugeDrawable.cs`: renderizacao visual
- `MainPage.xaml`: interface principal do desafio

## Como executar

1. Abra `TraceFit/TraceFit.sln` no Visual Studio 2022.
2. Instale o workload do .NET MAUI.
3. Restaure os pacotes e rode na plataforma desejada.

## Status

README inicial publicado com base na estrutura atual do repositorio.