# Chordious para macOS — roadmap de paridade funcional

## Objetivo

Entregar uma aplicação macOS com 100% de paridade comportamental com o
`Chordious.WPF`, preservando os formatos de configuração e biblioteca, o domínio
musical, os ViewModels compartilhados, a licença MIT e a aplicação Windows.

Paridade de 100% significa que toda funcionalidade observável da versão WPF está
presente e validada no macOS. Similaridade visual pixel a pixel não é um requisito:
a interface deve respeitar as convenções do macOS sem alterar o resultado dos
fluxos.

## Estado do baseline

| Gate | Estado | Evidência |
| --- | --- | --- |
| macOS Apple Silicon | Concluído | Ambiente identificado como `osx-arm64` |
| SDK exigido | Concluído | .NET SDK 10.0.302 arm64 instalado persistentemente e fixado por `global.json`; runtime 8 mantido para compatibilidade |
| Domínio no macOS | Concluído | 8 testes de `Chordious.CoreTest` aprovados em Release |
| UI multiplataforma | Em andamento | `Chordious.Desktop` em `net10.0` possui 24 dos 25 fluxos WPF concluídos, 1 parcial e nenhum pendente; o Diagram Editor cobre estilos, clipboard, fundo, tipo de marca, F5 e arraste de saída; menus nativos, atalhos principais, foco inicial e labels acessíveis estão ativos; 80 testes Desktop aprovados |
| CI multiplataforma | Concluído | GitHub Actions valida Core, Desktop e publicação `osx-arm64` no macOS, além da solução e dos pacotes existentes no Windows |
| Repositório e proveniência | Concluído | Fork público `vlosito/Chordious`, com `origin` apontando para o fork e `upstream` para `jonthysell/Chordious`; base validada em `main@b781057`, que contém `v2.8.0` e `2.8-official` |
| Governança da `main` | Concluído | Branch protegida para exigir PR, checks Debug/Release/macOS e resolução de conversas; force-push e exclusão bloqueados inclusive para administradores |
| Backlog herdado | Concluído | 20 issues upstream espelhadas no fork; 6 classificadas inicialmente como bloqueadoras da versão estável para macOS |

## Definição de pronto global

- Os 25 fluxos/janelas WPF possuem equivalente funcional no macOS.
- Bibliotecas e configurações abrem e salvam sem perda de dados, comprovadas por
  testes de round-trip com amostras reais.
- Chord Finder e Scale Finder produzem resultados equivalentes para o mesmo
  conjunto de entradas.
- SVG, PNG, GIF e JPG são exportados com dimensões, fontes, cores e
  DPI validados.
- Clipboard de texto, SVG e bitmap funciona com aplicativos nativos do macOS.
- Menus, atalhos, foco, diálogos, drag-and-drop e acessibilidade seguem as
  convenções do macOS.
- O aplicativo abre por duplo clique como `.app`, preserva dados entre execuções e
  passa por smoke real em máquina limpa.
- O pacote público, quando autorizado, é assinado, notarizado e validado pelo
  Gatekeeper.
- `Chordious.Core`, `Chordious.Core.ViewModel` e `Chordious.Desktop` não possuem
  referências WPF, WinForms ou `System.Windows`.

## Fases e gates

| Fase | Entrega | Gate de saída |
| --- | --- | --- |
| 0 — Baseline | SDK, testes, proveniência e amostras reais | Core verde no Mac e corpus de compatibilidade versionado |
| 1 — Spike vertical | Shell Avalonia, ViewModel real, configuração, diagrama real e exportação SVG | Executável inicia no Mac e conclui o fluxo vertical sem WPF |
| 2 — Biblioteca e editor | Biblioteca, coleções, diagramas, estilos, marcas, pestanas e rótulos | Criar, editar, salvar, reabrir e exportar sem perda |
| 3 — Busca musical | Chord Finder, Scale Finder e editores auxiliares | Resultados equivalentes ao WPF nas amostras de referência |
| 4 — Configuração completa | Instrumentos, afinações, intervalos, escalas, qualidades, preferências, importação e exportação | Configuração compatível e persistente entre execuções |
| 5 — Paridade de plataforma | Clipboard, menus, atalhos, diálogos, Finder, drag-and-drop, erros, ajuda e atualização | Matriz das 25 janelas e integrações integralmente verde |
| 6 — Distribuição | `.app`, `osx-arm64`, assinatura, notarização e DMG | Instalação e smoke em usuário/máquina limpos |

## Matriz de paridade

Legenda: `CONCLUÍDO`, `PARCIAL`, `PENDENTE`.

| Área WPF | Capacidades cobertas | Estado macOS |
| --- | --- | --- |
| Main | Inicialização, navegação, website, ajuda, opções e licenças | CONCLUÍDO — shell e ViewModel reais; menus nativos Chordious/Arquivo/Editar/Biblioteca/Ferramentas/Ajuda, `Cmd+F`, `Cmd+Shift+F`, `Cmd+,` e `Cmd+W`, website, ajuda, Options e Licenses foram validados no app real; a janela principal expõe nomes, ajuda e IDs estáveis ao NSAccessibility |
| Diagram Library | Árvore, coleções, criar, editar, excluir, clonar, copiar, mover, mesclar e estilos | CONCLUÍDO — navegação, CRUD, estilos, seleção múltipla e cópia, movimentação e mesclagem por seletor ou drag-and-drop; `Ctrl` e `Option` copiam, o gesto sem modificador move, coleções são mescladas e recargas rejeitam seleções obsoletas |
| Diagram Editor | Dimensões, título, marcas, pestanas, rótulos, estilos, preview e clipboard | CONCLUÍDO — criar/editar, título, cordas, casas, preview, marcas, rótulos, pestanas, reset, proteção contra perda, tipo padrão de marca, fundo do editor, F5/Esc, bitmap, bitmap escalado, SVG e arraste de saída estão funcionais; as 61 propriedades da janela WPF são acessadas pelo Style Editor Avalonia |
| Diagram Export | Escolha de caminho, SVG, PNG/GIF/JPG, lote, nomes parametrizados, colisões, sobrescrita e escala | CONCLUÍDO — testes e smoke no `.app` validaram os quatro formatos, escala 2×, lote com nomes repetidos, resolução de colisão e restauração byte a byte da configuração |
| Chord Finder | Instrumento, afinação, qualidade, opções, busca assíncrona, cancelamento e resultados | CONCLUÍDO — UI Avalonia cobre parâmetros e estilos, F5/Esc, seleção simples e múltipla, edição, clipboard, drag-and-drop de saída e salvamento; testes e smoke no `.app` validaram 49 resultados reais e atualização imediata da Biblioteca |
| Scale Finder | Instrumento, afinação, escala, opções, busca assíncrona, cancelamento e resultados | CONCLUÍDO — UI Avalonia cobre parâmetros e estilos, F5/Esc, seleção simples e múltipla, edição, clipboard, drag-and-drop de saída e salvamento; testes e smoke no `.app` validaram 6 resultados reais e atualização imediata da Biblioteca |
| Instruments | Gerenciador e editor de instrumentos e afinações | CONCLUÍDO — CRUD de instrumentos e afinações, cópia de afinação padrão, itens padrão somente leitura e atualização imediata dos seletores do Chord Finder e Scale Finder; testes e smoke no `.app` validaram instrumentos com 4 e 5 cordas, seleção estável após ordenação e confirmação/cancelamento de exclusão |
| Chord qualities | Gerenciador, editor e intervalos nomeados | CONCLUÍDO — itens padrão somente leitura, CRUD de itens do usuário, exemplos musicais, seleção estável após ordenação e atualização imediata do Chord Finder |
| Scales | Gerenciador, editor e intervalos nomeados | CONCLUÍDO — itens padrão somente leitura, CRUD de itens do usuário, exemplos musicais, seleção estável após ordenação e atualização imediata do Scale Finder |
| Options | Preferências, estilo global, resets, diretório temporário e defaults dos finders | PARCIAL — Settings, Styles, Finders e Config, incluindo Apply/Accept/Cancel, fundos, resets e Finder, estão funcionais; falta somente o grupo Updates, dependente do mecanismo macOS de atualização |
| Configuration | Persistência, importação, exportação, seleção de partes e importação legada | CONCLUÍDO — pickers nativos, seis partes selecionáveis, confirmação de sobrescrita, persistência, round-trip automatizado e real no `.app` e ChordLine automatizado estão funcionais |
| Element editors | Mark, Barre, Fret Label e Style | CONCLUÍDO — os quatro editores cobrem todas as propriedades existentes, níveis de herança, estilos locais, aplicar/salvar/cancelar e proteção contra perda; o Style Editor cobre as 61 propriedades herdáveis da versão WPF |
| Diálogos comuns | Confirmação persistente, informação, exceção, prompt e seleção de coleção | CONCLUÍDO — os cinco fluxos possuem equivalentes Avalonia, persistem as respostas aplicáveis, declaram ações padrão/cancelamento para Enter/Escape e recebem foco inicial seguro; os demais diálogos seguem a ordem macOS com Cancelar à esquerda da ação principal |
| Integrações | Clipboard de texto/bitmap, arquivos/pastas, URLs, Finder, fontes e atualização | PARCIAL — menus de aplicativo e janela, atalhos principais, foco inicial, labels NSAccessibility, texto/bitmap, fontes, pickers nativos, navegador padrão e pasta temporária no Finder estão funcionais; Diagram Editor, Chord Finder e Scale Finder oferecem arraste de saída com SVG, bitmap e PNG opcional, e a Library implementa copy/move/merge interno; falta validar os gestos e a interoperabilidade em destinos macOS reais |
| Distribuição | Bundle, ícone, assinatura, notarização, DMG e atualização | PENDENTE |

## Pendências restantes para paridade de 100%

Esta é a lista canônica do trabalho de migração ainda aberto. Issues herdadas que
não representam funcionalidade existente na versão 2.8.0 ficam fora desta lista.

### Janelas e fluxos WPF

Das 25 janelas de referência, 24 estão concluídas e nenhuma está ausente. Resta
1 equivalente parcial:

1. **Options — parcial:** implementar o grupo Updates após existir um mecanismo
   macOS capaz de consultar e instalar releases assinadas e notarizadas.

### Configuração e compatibilidade

2. Validar round-trip sem perda com corpus real de configurações, bibliotecas,
   estilos, instrumentos, qualidades, escalas e documentos legados da versão
   2.8.0.

### Integrações macOS

3. Validar em smoke manual real o drag-and-drop já implementado na Library, no
   Chord Finder, no Scale Finder e no Diagram Editor: mover sem modificador,
   copiar com `Option`/`Ctrl`, mesclar coleções e receber SVG, bitmap e arquivo
   PNG em aplicativos externos. A automação atual seleciona e clica na UI, mas
   não sustenta o gesto nativo Avalonia; por isso esse aceite permanece aberto.
4. Validar clipboard de texto/SVG, bitmap e bitmap escalado contra aplicativos
    nativos do macOS, além dos testes automatizados existentes.
5. Validar fontes, arquivos, pastas, recuperação de configuração inválida e
    mensagens de erro em uma conta macOS limpa.

### Atualização e distribuição

6. Criar o equivalente macOS do mecanismo de atualização WPF: verificação no
    início, consulta manual, canal de release, última verificação e instalação
    segura de uma versão assinada.
7. Definir nome público, bundle identifier, ícone definitivo e política para
    Macs Intel (`osx-x64` ou aplicativo universal).
8. Automatizar o bundle `.app` final, assinatura Developer ID, notarização,
    stapling, DMG e publicação dos artefatos no release público.
9. Executar Gatekeeper e smoke por duplo clique em usuário e máquina limpos,
    incluindo persistência entre execuções e atualização de uma versão anterior.

### Gates finais de equivalência

10. Comparar Chord Finder e Scale Finder com a versão WPF usando o mesmo corpus
    determinístico de entradas e resultados.
11. Validar SVG, PNG, GIF e JPG quanto a dimensões, DPI, fontes, cores,
    transparência e nomes de arquivo, incluindo interoperabilidade externa.
12. Executar uma matriz de smoke real dos 25 fluxos, integrações e atalhos e
    manter Core, Core.ViewModel e Desktop livres de WPF, WinForms e
    `System.Windows`.

## Política para issues herdadas

- A prioridade de implementação é concluir e comprovar a paridade funcional com
  a versão 2.8.0. As issues herdadas serão atacadas depois desse gate, exceto
  quando representarem bloqueio, perda de dados, segurança ou quando a própria
  migração da funcionalidade necessariamente resolver o defeito.
- As 20 issues abertas no projeto original foram espelhadas no
  [rastreador público do fork](https://github.com/vlosito/Chordious/issues), com
  link para a origem e preservação da autoria factual.
- As issues `#8` a `#13` formaram o conjunto inicial de bloqueadores conhecidos
  da versão estável para macOS; as `#9`, `#10`, `#11`, `#12` e `#13` estão resolvidas na PR
  draft `#1` e serão fechadas automaticamente após o merge.
- As demais issues são melhorias herdadas. Elas serão tratadas sem impedir a
  paridade da versão 2.8 quando não representarem regressão ou funcionalidade já
  existente no aplicativo Windows.
- O gate de lançamento é zero issue aberta com o rótulo
  `macos-release-blocker`, e não a eliminação artificial de todo o backlog de
  melhorias.

## Inventário das 25 janelas de referência

1. Main
2. Chord Finder
3. Scale Finder
4. Diagram Library
5. Diagram Editor
6. Diagram Export
7. Diagram Collection Selector
8. Diagram Style Editor
9. Diagram Mark Editor
10. Diagram Barre Editor
11. Diagram Fret Label Editor
12. Instrument Manager
13. Instrument Editor
14. Tuning Editor
15. Named Interval Manager
16. Chord Quality Editor
17. Scale Editor
18. Options
19. Advanced Data
20. Config Parts
21. Confirmation
22. Information
23. Exception
24. Text Prompt
25. Licenses

## Política de SDK e target frameworks

- O repositório usa SDK .NET 10 LTS, selecionado por `global.json` com
  `rollForward=latestFeature`.
- `Chordious.Desktop` e `Chordious.DesktopTest` miram `net10.0` para usar o LTS
  atual e o toolchain compatível com os analisadores do Avalonia 12.
- `Chordious.Core`, `Chordious.Core.ViewModel`, `Chordious.CoreTest` e
  `Chordious.WPF` permanecem em `net8.0` nesta etapa para reduzir o impacto da
  contribuição upstream e manter um gate explícito de compatibilidade.
- A migração do código compartilhado para `net10.0` deve ocorrer somente após
  validação no Windows e decisão do mantenedor do projeto original.

## Critério de execução por incremento

Cada incremento deve:

1. reutilizar o ViewModel compartilhado sempre que ele não depender de UI;
2. criar a menor fronteira de plataforma necessária;
3. incluir teste de domínio, serviço ou round-trip proporcional ao risco;
4. executar build Release do `Chordious.Desktop` no macOS;
5. executar smoke no aplicativo real quando houver mudança visual ou de interação;
6. atualizar esta matriz somente com evidência executada.

## Decisões ainda necessárias para distribuição

- Nome público e bundle identifier do fork.
- Ícone definitivo.
- Suporte ou não a Macs Intel (`osx-x64`).
- Disponibilidade de Apple Developer ID para assinatura e notarização.

A distribuição será pública pelo fork `vlosito/Chordious`, preservando a licença
MIT, os avisos de copyright e a proveniência do projeto original.
