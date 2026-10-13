# Rastro do Dossel

Game Design Document

**Trabalho de Game Development — Ciência da Computação**

**Versão:** 0.6 — atualização com o que foi implementado e testado na primeira entrega.

**Integrantes:** Matheus Braga, Moruan Mattar, Nícolas Muciaccia e Nícolas Ramos. **Turma e professor:** a preencher pela equipe.

**Data desta revisão:** 13 de outubro de 2026.

**Proposta:** jogo de plataforma em 3D, visto lateralmente em terceira pessoa, sobre um guerrilheiro e guia que atravessa uma selva armadilhada para restabelecer a comunicação entre comunidades.

**Plano de entrega:** cinco níveis no jogo final; somente o nível 1 implementado no primeiro bimestre. O controle do personagem concentra o trabalho de jogabilidade. Os demais níveis reutilizam os mesmos sistemas.

**Estado do documento:** o nível 1 está implementado e jogável do menu à tela de conclusão, com formas provisórias no lugar dos modelos de terceiros. As seções de mecânica, interface, base técnica e documentação das funções descrevem o que existe no projeto. Os níveis 2 a 5, o Terrain, o NPC e o NavMesh continuam como planejamento do segundo bimestre. O quadro abaixo resume a situação; a seção "Verificação de atendimento ao enunciado" detalha cada item.

| Frente | Situação em 13/10/2026 |
|---|---|
| Controle do Nilo, câmera e física | Implementado e testado no Editor |
| Nível 1: perigos, estação, saída, tutorial e falas | Implementado e testado no Editor |
| Menu, pausa, falha com Reiniciar e conclusão parcial | Implementado e testado no Editor |
| Conceitos dos tutoriais (Translate, AddForce, rotação, torque, vetor de prefabs, tags) | Implementado |
| Animações, partículas e sons | Implementado (personagem e efeitos provisórios) |
| 70 assets por nível | Nível 1 montado: 70 IDs do catálogo na cena, 73 modelos distintos de terceiros (CC0); critério a confirmar com o professor |
| Teste completo com teclado no computador do laboratório | Pendente |
| Níveis 2 a 5, Terrain, NPC, NavMesh, pós-processamento | Segundo bimestre |

**Repositório:** o projeto Unity, este GDD e o catálogo de assets estão no repositório privado da equipe no GitHub (MatheusBragaC/rastro-do-dossel).

**Organização:** a história de Nilo e sua relação com as cinco fases vêm primeiro. Em seguida estão as lentes de design, a visão de jogo e as especificações do modelo. Ao final, o documento relaciona as exigências acadêmicas, os recursos e as entregas.

---

## História de Nilo o Sabiá

### Antes da ocupação

Nilo nasceu em Várzea Alta, uma comunidade cercada por rios e mata fechada. Aprendeu com a mãe a reconhecer mudanças de tempo pelo som dos pássaros e com o avô a encontrar trilhas que não aparecem nos mapas. Quando começou a acompanhar as viagens entre os povoados, sua tarefa era simples: caminhar à frente, procurar terreno firme e avisar quando os demais podiam passar. Recebeu o apelido de Sabiá porque usava um assobio curto para indicar que o caminho estava livre.

Já adulto, tornou-se guia de travessias. Conhecia as raízes que suportavam peso, as encostas que cediam depois da chuva e os pontos em que um salto encurtava o caminho. Também ajudava a conservar passagens: recolocava sinais derrubados, protegia mudas nas margens e conduzia os viajantes por trilhas abertas, evitando que cada grupo destruísse um novo pedaço da mata. Para Nilo, conhecer a floresta significava cuidar das condições que permitiam viver nela.

Essa rotina mudou quando uma força armada chamada Vigília ocupou o vale. Seus oficiais disseram que precisavam de mapas para transportar mantimentos e proteger as comunidades. Nilo aceitou indicar algumas rotas em troca da liberação de uma passagem usada por sua família. Não imaginava que os mesmos pontos seriam usados para instalar bloqueios, retirar equipamentos comunitários e controlar a circulação de pessoas.

Quando percebeu o que acontecia, os mapas já estavam nas mãos da Vigília. Marcas que havia feito nas árvores continuaram no lugar, agora ao lado de estacas e mecanismos de armadilha. O viveiro da comunidade perdeu sua ligação com o rio; o antigo sistema de rádio foi desmontado. Nilo não construiu os bloqueios, mas reconhece que seu conhecimento ajudou a torná-los possíveis. Desde então, evita falar sobre o mapa e teme tomar outra decisão que prejudique os moradores.

### O guerrilheiro que abre caminhos

Nilo passou a integrar uma pequena resistência formada por moradores do vale. Seu papel é transportar suprimentos, observar os caminhos e orientar travessias. O termo guerrilheiro descreve sua ligação com esse grupo e sua atuação na selva. A experiência jogável se concentra em mobilidade, observação e auxílio às comunidades; seu equipamento principal é um rádio, acompanhado de mochila, cantil e ferramentas simples.

Ele é prático, fala pouco e demonstra atenção antes de agir. A culpa o tornou hesitante quando precisa orientar outras pessoas, embora seu corpo ainda conheça os movimentos da mata. No início do jogo, aceita executar uma entrega porque acredita que uma missão com destino definido exige menos decisões. A viagem vai obrigá-lo a perceber que transmitir informações e assumir escolhas também fazem parte de sua responsabilidade.

Mara, amiga de infância e técnica de rádio, mantém contato com as comunidades. Ela sabe que Nilo conhece os caminhos ocupados, mas não conhece todos os detalhes do acordo que ele fez. Não funciona como uma voz que resolve cada obstáculo. Oferece contexto, recebe relatos e cobra informações úteis. A confiança entre os dois cresce à medida que Nilo deixa de esconder o passado.

### A noite da missão

Mara intercepta a notícia de que a travessia do rio será fechada ao amanhecer. Várzea Alta e dois povoados vizinhos ainda não receberam o aviso. A rede de rádio está fragmentada: pequenos aparelhos comunicam trechos próximos, mas somente a antena da serra pode alcançar todo o vale. Para ativá-la, alguém precisa levar um módulo transmissor e restabelecer pontos locais de alimentação e retransmissão no caminho.

Nilo recebe o módulo e parte. O trajeto cruza a trilha da infância, uma margem degradada, o corredor onde seu mapa foi usado, um posto comunitário e a subida da antena. Em cada local, encontra uma intervenção pequena que pode facilitar a passagem dos moradores. São ações possíveis para uma pessoa de passagem: abrir o acesso a um viveiro, destravar um filtro, recolher material descartado, liberar uma barreira de contenção e proteger uma área de recuperação do solo.

O amanhecer dá urgência à história. Não haverá um cronômetro que cause derrota. O jogador pode parar em trechos seguros para observar uma armadilha ou ler uma fala. A iluminação e os diálogos mostram a noite avançando, sem transformar a primeira experiência em uma corrida contra o relógio.

### Conflito e desfecho

O conflito externo é atravessar o território controlado pela Vigília. O conflito interno é aceitar que o silêncio sobre o mapa também impede os outros de se protegerem. Na metade da jornada, Nilo reconhece um corredor de caça construído a partir de suas marcações. Ele conta a Mara o que fez e identifica uma passagem alternativa. Ela pede que descreva essa rota com precisão para que possa ajudar quem ainda está no vale.

No posto comunitário, Nilo encontra Ivo, um morador que reúne materiais para reabrir o caminho. O encontro mostra que a recuperação da região depende de várias pessoas. Nilo ajuda com uma tarefa curta e segue; Ivo continua o serviço. Essa situação prepara a chegada à antena: o objetivo não é ser o único capaz de atravessar, mas permitir que outros saibam por onde ir.

Na transmissão final, Nilo anuncia o fechamento do rio, descreve a rota alternativa e informa quais pontos já podem receber os moradores. Uma primeira voz responde; depois chegam outras confirmações. Mara começa a organizar a travessia. Nilo deixa seu mapa atualizado no posto da antena e faz novas marcas para o próximo grupo. O jogo termina com o caminho ainda exigindo cuidado, mas com o conhecimento compartilhado.

## A história nas cinco fases

### Nível 1 Trilha encoberta

Nilo sai de Várzea Alta por uma trilha familiar. Um pequeno trecho de caminhada apresenta o personagem e o rádio. Raízes, passagens baixas e um fosso ensinam a ler o terreno. Perto do primeiro bloqueio, ele reconhece sua marca antiga numa árvore. O caminho que prometia ser seguro agora termina em estacas.

No viveiro abandonado, Nilo abre uma caixa de ferramentas e libera o acesso às mudas. O rádio recebe uma mensagem de Mara. Ele avisa que a trilha está comprometida, liga um sensor de passagem e decide seguir até a margem para entender a extensão dos bloqueios. O nível termina no posto de observação. A entrega do primeiro bimestre encerra aqui, com uma mensagem clara de conclusão da demonstração.

**Mudança do personagem:** Nilo deixa de tratar a missão como uma simples entrega e começa a observar as condições de quem virá depois. **Relação com o controle:** os movimentos que aprendeu como guia aparecem nas cinco situações de habilidade do tutorial.

### Nível 2 Margem ferida

A margem do rio apresenta solo exposto, resíduos e uma estrutura de filtragem comunitária parada. Pequenas passagens sobre raízes e pedras conduzem a uma área segura. Nilo destrava o filtro e recoloca o painel solar de um equipamento local em funcionamento por uma interação simples. A água não muda instantaneamente em todo o rio: um reservatório e o indicador da estação mostram a intervenção localizada.

Ao olhar para o outro lado, vê que a travessia principal está fechada antes do horário anunciado. Mara informa que alguns moradores já voltaram para casa. Nilo precisa alcançar a serra e avisar os demais antes que tentem a mesma passagem. A fase acrescenta combinações de obstáculos conhecidos, mantendo espaços para o jogador recuperar o controle.

**Mudança do personagem:** o problema deixa de ser uma lembrança pessoal e se torna uma consequência visível para a comunidade. **Relação com o controle:** pousos mais precisos e alternância entre caminhar e correr ajudam a atravessar uma margem estreita.

### Nível 3 Corredor de caça

As armadilhas estão instaladas exatamente nos pontos que Nilo marcou no mapa antigo. Um tronco cruza a passagem em intervalos previsíveis. Há estacas e galhos baixos, separados por pontos de espera. Guardas da Vigília aparecem numa passarela distante e reforçam a presença da ocupação.

Depois da travessia, Nilo encontra uma estação de retransmissão cercada por material descartado. Ele reúne os resíduos em um recipiente e ativa o repetidor. Do ponto alto, reconhece uma passagem acima do corredor. Em uma conversa curta, admite a Mara que foi ele quem forneceu o mapa original. Em seguida, descreve a rota alternativa. O cenário mostra o desvio ao fundo, sem exigir uma segunda campanha jogável.

**Mudança do personagem:** Nilo assume seu papel e transforma o conhecimento que escondia em informação útil. **Relação com o controle:** esperar o momento do tronco exige observar o ambiente, uma habilidade tão importante quanto pressionar o salto.

### Nível 4 Posto das raízes

O posto reúne viveiro, oficina e suprimentos de moradores. Ivo prepara a saída, mas uma encosta instável ameaça a passagem. Nilo libera uma pequena barreira já montada para conter o material solto e conecta uma bateria de apoio. A ação usa o mesmo comando de interação aprendido antes.

Ivo caminha até um ponto de serviço e depois ao abrigo. Esse percurso curto permite demonstrar um NPC com NavMesh na entrega final. Nilo não precisa escoltá-lo nem esperar que uma inteligência artificial complexa encontre a saída. Enquanto Ivo permanece trabalhando, o jogador atravessa o último trecho e avista a antena.

**Mudança do personagem:** Nilo aceita colaborar com pessoas que continuarão a recuperação do vale. **Relação com o controle:** o nível alterna uma área segura de conversa com uma sequência curta de salto e deslize, sem ensinar novas teclas.

### Nível 5 A antena

O rádio perde clareza durante a subida. O jogador já conhece os perigos, e a última fase exige combiná-los com calma. A proximidade do amanhecer altera a iluminação, mas a trilha permanece legível. Uma faixa de solo protegido, cercada para regeneração, orienta o caminho ao redor da estação.

Nilo fecha a entrada indevida dessa área, instala o módulo na antena e transmite o aviso. A cena mostra a resposta de Mara e das comunidades por legendas e sons de rádio. O encerramento retoma o assobio de Sabiá: agora ele sinaliza um caminho que outras pessoas também conhecem.

**Mudança do personagem:** ele conclui a missão e compartilha a responsabilidade de orientar os demais. **Relação com o controle:** a sequência final testa o domínio das mesmas ações praticadas desde a primeira fase, sem um chefe ou sistema de combate adicional.

### Roteiro das falas essenciais

**Abertura do nível 1 — Mara, pelo rádio, na saída de Várzea Alta:** “A travessia fecha ao amanhecer. Leve o módulo até a antena. Eu acompanho enquanto houver sinal.” Nilo responde: “Conheço o começo da trilha. Vou avisando.” A cena utiliza personagens parados e uma legenda breve.

**Marca antiga do nível 1 — Nilo, diante da árvore marcada:** “Essa marca fui eu que fiz. As estacas vieram depois.” A fala acontece no chão seguro, antes do fosso, e liga a história ao primeiro perigo construído.

**Reservatório do nível 2 — Mara, pelo rádio:** “O pessoal usa essa água na travessia.” Nilo responde: “Vou liberar a estação. O resto precisa da equipe do posto.” A fala delimita o efeito da tarefa e evita sugerir que uma ação recupera todo o rio.

**Saída do nível 3 — Nilo, junto ao repetidor:** “Fui eu quem mostrou essas trilhas à Vigília. A passagem antiga fica acima do corredor.” Mara responde: “Então me diga cada referência. Vamos orientar os outros.” O diálogo torna explícita a transformação do personagem.

**Posto do nível 4 — Ivo, junto à oficina:** “Com a barreira no lugar, dá para trabalhar aqui.” Nilo responde: “A rota pela encosta está livre. Mara tem as referências.” Ivo segue até o ponto de serviço; o jogador mantém o controle.

**Final do nível 5 — Nilo, na antena:** “Várzea Alta, não cruzem pelo rio. Sigam as marcas novas na encosta e passem pelo posto.” Mara responde: “Recebido, Sabiá. Vamos avisar os outros.” Uma voz comunitária confirma o recebimento.

As falas são texto obrigatório no planejamento e voz opcional na produção. Cada interação acontece fora da zona de perigo. O jogador pode avançar a legenda com E depois de terminar a interação, sem repetir a ação já concluída. Não haverá diálogo ramificado.

## Mundo e personagens

### Geografia e aparência do vale

Várzea Alta é o ponto de partida, mostrado em uma área pequena. A trilha leva à margem do rio, sobe por um corredor estreito e encontra o posto comunitário antes de alcançar a serra. As conexões aparecem na direção de deslocamento, em placas, na vista da antena e em elementos do cenário. As cinco fases são cenas lineares distintas, conectadas pela narrativa.

A ambientação combina mata estilizada, construções simples de madeira e equipamentos reaproveitados. O caminho jogável usa contraste e espaço livre. Árvores de fundo podem ser densas; plantas do primeiro plano não escondem o pé do personagem ou a borda de um fosso. Marcas antigas da Vigília e marcas novas da comunidade devem ter formatos diferentes, além de cores diferentes.

O mundo usa um recorte fictício de floresta e comunidades. Nomes, conflito e personagens não representam um episódio histórico específico. A reconstrução aparece em ações locais e no cenário, sem transformar o jogo em uma simulação ambiental de larga escala.

### Nilo

**História e função:** antigo guia, integrante da resistência e portador do módulo de rádio. É o único personagem jogável. Sua trajetória liga as armadilhas às consequências do mapeamento e às ações de recuperação.

**Personalidade e relações:** observador, reservado e disposto a ajudar, mas inicialmente receoso de orientar pessoas. Mara o conhece desde a infância; Ivo representa os moradores que dependem de informações confiáveis. A Vigília usa caminhos que ele revelou.

**Aparência e habilidades:** adulto, roupa de campo simples, mochila compacta, rádio visível e lenço de identificação. A silhueta precisa continuar legível quando salta ou se abaixa. Sabe caminhar, correr, pular, deslizar e observar o momento de atravessar. Não há habilidades desbloqueáveis.

**Modelo da primeira entrega:** o Nilo foi montado pela equipe com primitivas do Unity, no visual de guerrilheiro da selva: chapéu de selva verde-oliva, camisa camuflada com colete, calça cáqui, botas, cinto com cantil, lenço laranja de identificação e mochila com saco de dormir, rádio, antena e LED. Não carrega arma. Braços e pernas têm pivô no ombro e no quadril para as animações. O laranja do lenço destaca o personagem do fundo verde e marrom. Fica no prefab `Assets/Prefabs/Nilo.prefab`.

**Animações implementadas:** parado, caminhada, corrida, salto, queda, deslize, interação e falha, criadas pela equipe em `Assets/Animations` e controladas por `Nilo.controller`. Caminhada e corrida balançam braços e pernas em oposição; a interação inclina o tronco e estende o braço direito; a falha derruba o Nilo para trás. A aterrissagem é representada pela poeira e pelo som de pouso, sem clipe próprio. Na entrega final, melhorar encaixe, velocidade e efeitos, ou trocar por um modelo com rig, se o professor autorizar.

### Mara

**História e função:** técnica que mantém a comunicação da resistência. Reconstrói equipamentos com peças disponíveis e coordena os avisos. Sua presença pode ser apenas um retrato estático e legendas de rádio, evitando um segundo personagem animado em todas as fases.

**Personalidade e relação:** objetiva, acolhe relatos concretos e exige que Nilo compartilhe o que sabe. Não resolve os saltos nem dá ordens a cada segundo. Sua habilidade é interpretar sinais e conectar pessoas. Se houver um modelo na abertura, bastam as animações parado e interação com rádio.

### Ivo e a Vigília

**Ivo:** morador responsável pelo posto, com roupa de trabalho e ferramenta identificável. Sua história é apresentada pela oficina e por uma fala. Usa animações parado e caminhada e percorre três pontos seguros. Ele demonstra o NPC amigável da entrega final sem criar uma missão de escolta.

**Vigília:** força antagonista identificada por placas, postos de observação e guardas distantes. Os guardas podem compartilhar um modelo e uma animação de patrulha curta. Não perseguem o jogador na versão prevista. Sua função é contextualizar os bloqueios; o contato jogável com perigo vem das armadilhas.

## Lentes de experiência e surpresa

### Experiência essencial

A experiência pretendida é sentir que o domínio do corpo de Nilo permite atravessar um lugar difícil e produzir uma consequência útil. O jogador deve perceber uma relação clara entre observar, decidir e executar. Um salto bem posicionado, um deslize sob um galho e uma espera curta diante do tronco precisam ser compreensíveis antes e depois da ação.

**Movimento confiável:** entradas respondem imediatamente nas condições válidas; animações acompanham o estado. A representação é o controle consistente da cápsula, do salto e do colisor baixo. O teste é repetir o mesmo movimento no mesmo obstáculo e obter um resultado previsível.

**Perigo legível:** estacas, galhos e troncos têm silhuetas próprias. O jogador vê a área de pouso e um lugar de espera. A representação combina contraste, espaço de câmera e um aviso sonoro no perigo móvel. O teste é conseguir identificar o motivo de uma falha sem explicação oral da equipe.

**Responsabilidade compartilhada:** as marcas antigas e novas, as falas de Nilo e a resposta das comunidades representam sua transformação. O teste narrativo é o jogador compreender quem forneceu o mapa, por que leva o módulo e o que muda ao final da transmissão.

**Recuperação local:** cada estação ambiental tem um estado visual antes e depois. A intervenção ocupa poucos segundos num trecho seguro. O teste é o jogador reconhecer a mudança sem precisar ler o GDD. Os efeitos ficam restritos à área que foi atendida.

### Surpresa com regras consistentes

**Primeira surpresa:** a marca de confiança de Nilo aparece junto às estacas no nível 1. Ela altera o significado de um elemento já apresentado, sem introduzir uma armadilha invisível. A área segura permite perceber a descoberta antes de continuar.

**Segunda surpresa:** o rio já está bloqueado no nível 2. A revelação é visual e narrativa; a rota jogável continua possível. Ela aumenta a urgência sem exigir que o jogador refaça uma fase.

**Terceira surpresa:** no nível 3, o corredor confirma a relação entre o mapa e as armadilhas. Logo depois, o ponto alto revela a passagem alternativa. A surpresa nasce da leitura do cenário e da conversa, sem depender de um novo sistema.

**Quarta surpresa:** o posto não está abandonado. Ivo continua trabalhando no nível 4. A mudança no som ambiente e um movimento simples do NPC antecedem a fala.

**Quinta surpresa:** a antena recebe várias respostas, não apenas a de Mara. A recompensa final é ouvir que a informação começou a circular. O amanhecer e as marcas novas completam a cena.

As regras também permitem descobertas pequenas: caminhar pode facilitar um pouso, esperar um ciclo pode tornar um trecho mais simples e o deslize pode atravessar uma abertura que parecia bloqueada. O layout das armadilhas é fixo. A aleatoriedade fica em pequenos objetos e efeitos que não mudam a chance de sobrevivência.

## Visão geral do jogo

### Conceito público e gênero

Rastro do Dossel é um jogo de plataforma e evasão em terceira pessoa, produzido com cenário e personagens 3D. A câmera lateral e o movimento restrito a um plano reduzem a dificuldade de controle e a complexidade de construção das fases. Nilo atravessa armadilhas e realiza intervenções breves para levar um aviso às comunidades do vale.

O público inicial são colegas da disciplina e jogadores adolescentes ou adultos familiarizados com teclado. O conteúdo apresenta ocupação e perigo sem violência gráfica. Essa descrição orienta a criação; não equivale a uma classificação indicativa oficial. O jogo é individual e funciona sem conexão durante a partida.

O jogo final terá cinco níveis de aproximadamente dois a três minutos cada em uma travessia bem-sucedida. A meta total é de dez a quinze minutos, podendo aumentar com tentativas. A demonstração do primeiro bimestre contém somente o nível 1, sua abertura e uma tela própria de conclusão parcial.

### Pilares e fluxo resumido

**Personagem responsivo:** aperfeiçoar movimento, salto e deslize antes de ampliar a variedade de conteúdo. O desafio vem da combinação e da leitura dessas ações.

**Sistemas reutilizados:** um controlador, três famílias de armadilhas, um sistema de interação e a mesma estrutura de início, falha e conclusão nas cinco cenas.

**Narrativa no percurso:** cada ambiente apresenta uma consequência da ocupação e uma informação necessária à missão. Textos aparecem em áreas seguras.

**Progressão compreensível:** começar com ações isoladas, combiná-las gradualmente e concluir com uma sequência curta de domínio. Cada fase contém espaços de recuperação.

O jogador entra pelo menu, inicia a missão, aprende as ações no nível 1, atravessa os demais trechos e alcança a antena. Uma falha permite reiniciar a fase atual. Uma conclusão carrega a próxima fase; no nível 5, abre o encerramento. O menu de pausa permite continuar, reiniciar ou voltar ao título.

### Limites da proposta

Não estão previstos mundo aberto, multiplayer, inventário em grade, fabricação de itens, árvore de habilidades, combate armado, chefe, plataformas com física de corda ou geração procedural de fases. As tarefas ambientais usam o mesmo comando e alteram estados simples de objetos. A presença de tecnologia no tema se manifesta em equipamentos e objetivos reconhecíveis.

O NPC e o NavMesh pertencem à entrega final exigida pelo trabalho. A primeira etapa mantém o foco no nível 1 e nos conceitos dos tutoriais. Arte, sons e animações serão escolhidos conforme a autorização do professor e registrados por origem. A originalidade está na história, composição das fases, combinação das mecânicas e implementação do projeto.

## Jogabilidade e mecânicas

### Objetivos progressão e desafios

O objetivo geral é ativar a antena e transmitir o aviso. Em cada nível, o jogador precisa atravessar o percurso, concluir a estação de serviço local e interagir com o terminal de saída. A estação reúne a ação ambiental e o equipamento tecnológico daquela fase em um único ponto, para evitar uma lista longa de missões.

A progressão é linear. Os comandos estão disponíveis desde o começo; não há desbloqueios por experiência. O nível 1 ensina cinco habilidades práticas. O nível 2 pede precisão; o nível 3 enfatiza o tempo das armadilhas; o nível 4 alterna cooperação e travessia; o nível 5 combina tudo.

Não há quebra-cabeças de senha, ordenação de peças ou tentativa aleatória. O pequeno problema de cada trecho é identificar qual ação resolve a passagem. Estações usam E com um aviso visível. Se a saída for acionada antes da estação local, a interface explica o objetivo pendente e permite retornar pelo caminho seguro.

### Ciclo de jogo

Observar o próximo trecho, escolher a velocidade e a ação, atravessar, alcançar uma área segura, perceber uma mudança narrativa e seguir. Um erro fatal leva à tela de falha. A retomada recarrega a cena e restaura os objetos locais ao estado inicial, evitando regras de persistência diferentes entre jogador e cenário.

Cada tentativa tem no máximo poucos minutos. O projeto começa sem checkpoints. Se a repetição do início gerar frustração nos testes, a primeira correção é encurtar o trecho ou reorganizar os desafios. Um checkpoint fixo só será considerado depois que o fluxo básico estiver estável e se couber no cronograma.

### Controles

**A e D ou setas esquerda e direita:** caminhar no eixo horizontal. **Shift esquerdo enquanto move:** correr. A corrida é apenas uma mudança de velocidade, sem resistência ou medidor de energia.

**Espaço:** salto de altura fixa, disponível quando apoiado no chão. **Ctrl esquerdo ou S:** iniciar um deslize curto no chão. **E:** interagir com a estação ou avançar a fala em uma área segura.

**R:** reiniciar a fase. **Esc:** pausar ou continuar. Menus também oferecem botões visíveis, incluindo obrigatoriamente Reiniciar. O mouse é necessário apenas para clicar na interface; a movimentação do personagem usa teclado.

### Física e deslocamento

O personagem utiliza Rigidbody e colisor de cápsula, com profundidade e rotação física limitadas para permanecer no plano da fase. O movimento horizontal deve respeitar colisões; a implementação prevista atualiza o movimento físico no passo de física. A equipe não deve movimentar o mesmo Rigidbody simultaneamente por transform.Translate e por forças concorrentes.

O conceito Translate, exigido nos tutoriais, será demonstrado pelo controlador de um sinalizador sem Rigidbody dinâmico, deslocado entre limites conhecidos numa área segura. Obstáculos com colisão móvel usarão uma solução coerente com o sistema físico escolhido. AddForce será empregado no salto. A rotação aparece na orientação visual de Nilo e num sinalizador do posto. AddTorque movimenta pequenos detritos físicos ao abrir a caixa de ferramentas.

Valores implementados: caminhar a 3 unidades por segundo; correr a 5; salto com 6 unidades por segundo de velocidade vertical, aplicado com `AddForce` em `ForceMode.VelocityChange` para não depender da massa; gravidade de 12 unidades por segundo ao quadrado, definida uma vez nas configurações de física do projeto; deslize de 0,55 segundo a 5,5 unidades por segundo. A cápsula tem 1,8 unidade de altura e 0,35 de raio, com o pivô nos pés, e baixa para 0,9 no deslize. O Rigidbody trava rotação e profundidade (eixo Z) e usa um material sem atrito para não grudar em paredes. Todos os valores ficam editáveis no Inspector do Nilo.

Medições feitas no Editor em 9/10/2026: caminhada a 3,00 unidades por segundo e deslize de 2,97 unidades de distância, contra 3,0 previstas. A altura e o alcance do salto não foram medidos com instrumento; foram confirmados na prática, saindo do fosso de 1 unidade e passando o vão de 1,8 unidade correndo.

Com esses valores ideais e piso na mesma altura, a trajetória teria cerca de um segundo de voo e alcance aproximado de cinco unidades correndo. Os primeiros vãos devem ficar muito abaixo desse limite, entre 1,5 e 2 unidades. A colisão, o tamanho da cápsula e a altura de pouso precisam ser considerados em testes reais antes de fixar as distâncias.

Não alterar Physics.gravity por multiplicação a cada reinício: isso pode acumular mudanças entre carregamentos. Definir a gravidade uma vez de forma explícita ou aplicar uma solução local consistente. O terceiro protótipo serve de referência conceitual para o salto, mas seus valores não devem ser copiados sem revisão.

### Salto e contato com o chão

O salto só é autorizado com apoio válido sob os pés. Encostar de lado num tronco não deve contar como chão. A detecção combina a camada de solo com a direção do contato ou uma consulta curta sob a cápsula. O acionamento do botão é registrado uma vez; manter Espaço pressionado não cria saltos automáticos.

A altura não depende de quanto tempo a tecla fica pressionada. O personagem pode ajustar o movimento horizontal durante o salto, com velocidade previsível. A animação de queda começa quando a velocidade vertical muda de sentido. O pouso restaura o estado de chão e produz um efeito discreto de poeira ou folhas.

**Como foi implementado:** a consulta de chão lança uma esfera curta para baixo a partir da base da cápsula, só contra a camada Ground, e aceita superfícies com normal vertical acima de 0,7. Enquanto o Nilo sobe, nada conta como chão. Para o controle parecer justo, o salto tem duas tolerâncias de 0,1 segundo: ainda vale logo depois de sair da borda e vale se Espaço for apertado pouco antes do pouso. Os testes confirmaram que encostar de lado num bloco em pleno ar não habilita salto e que manter Espaço pressionado não repete o salto.

### Deslize

O deslize reduz a altura e ajusta o centro do colisor por um intervalo curto. Só começa no chão e não concede invulnerabilidade. O personagem mantém uma velocidade horizontal definida na direção em que estava olhando. Apertar novamente durante a ação não prolonga indefinidamente o tempo.

Ao terminar, o sistema consulta o espaço acima. Se houver um galho, mantém a postura baixa até poder levantar e permite sair da passagem. Esse cuidado evita que o colisor normal reapareça dentro de um obstáculo. A animação precisa representar a postura; o controle não deve esperar que uma animação termine para reconhecer uma colisão fatal.

**Como foi implementado e testado:** abaixado, o Nilo não salta e caminha só em velocidade de caminhada. A consulta de espaço acima testa a cápsula em pé contra tudo, menos a própria camada do Nilo e a camada dos detritos decorativos. No teste, um deslize iniciado 2,5 unidades antes do galho terminou embaixo dele; o Nilo continuou abaixado, recusou o salto e, com D pressionado, saiu andando e levantou depois do galho.

### Armadilhas e falha

**Fosso com estacas:** região estática marcada como perigo. As estacas têm bordas visíveis e uma área de pouso livre. Tocar a região fatal encerra a tentativa. Variações de comprimento não exigem scripts diferentes.

**Galho ou barra baixa:** obstáculo que pede deslize. O formato e a altura são consistentes. Nos primeiros exemplos, há área livre antes e depois; em combinações posteriores, existe tempo suficiente para voltar à postura normal antes do salto.

**Tronco móvel:** perigo que cruza um trecho entre dois limites fixos. O ciclo e a velocidade são previsíveis. O jogador observa uma passagem completa a partir de um ponto protegido. O tronco nunca aparece aleatoriamente sobre o personagem.

Contato fatal desativa a entrada de jogo, dispara uma animação ou pose curta, reproduz som e partículas e abre o painel de falha. A função de falha ignora chamadas seguintes enquanto o estado já estiver encerrado. Não há pontos de vida, cura ou perda de objetos entre tentativas.

**Como foi implementado:** o fosso tem uma zona fatal (trigger) entre as estacas e o fundo. O galho baixo é um obstáculo sólido, não fatal: bloqueia quem tenta passar em pé. O tronco móvel é um pilão suspenso que desce e sobe na vertical, porque na câmera ortográfica um movimento em profundidade não seria legível. O ciclo é fixo: 1,8 segundo erguido (passagem aberta), 0,35 segundo descendo, 0,6 segundo embaixo e 0,35 segundo subindo. Um aviso sonoro toca quando ele começa a descer. Uma zona de queda abaixo de todo o mapa também encerra a tentativa, para nenhuma queda ficar sem fim.

### Objetos interações e economia

A estação de cada nível possui um objeto de ação ambiental, um equipamento tecnológico e um indicador. O jogador entra na área, vê “E — realizar ação”, pressiona E e observa a mudança. O evento marca a estação como concluída e atualiza o objetivo. Acionamentos repetidos não duplicam progresso ou efeitos.

**Como foi implementado:** estação e terminal de saída herdam do mesmo componente base (`Interactable`), que detecta o Nilo por distância (1,8 unidade), mostra o aviso e chama a ação ao apertar E. A estação troca objetos ativos e inativos (tampa fechada por aberta, portão fechado por aberto, LED vermelho por verde) e dispara um evento configurável no Inspector, usado para os detritos, a poeira e o som. Quando a ação tem efeito, o Nilo toca a animação de interação. O aviso de um objeto só é apagado por ele mesmo, para dois objetos próximos não apagarem o aviso um do outro.

O módulo transmissor acompanha Nilo como parte de sua mochila até a antena. Não pode ser perdido, vendido ou lançado. Resíduos e ferramentas não exigem um inventário: uma interação agrupa a tarefa em uma animação curta e muda o estado do cenário.

Não existe economia monetária, loja ou pontuação competitiva. O retorno ao jogador é espacial e visual: alcançar um ponto seguro, reativar uma estação e abrir a próxima etapa. Um contador de estação concluída pode aparecer no HUD, mas não atribui preço à recuperação ambiental.

### Combate opções e repetição

O conflito com a Vigília ocorre por evasão das armadilhas. Não há ataque do jogador, munição ou mira. Guardas de fundo não bloqueiam o percurso e não entram na área jogável. Isso permite concentrar o polimento nos movimentos de Nilo.

As opções previstas são volume geral e retorno ao menu. Legendas ficam habilitadas. Na primeira entrega, o retorno ao menu existe na pausa, na falha e na conclusão; o controle de volume ainda não foi implementado. Remapeamento de teclas e suporte a controle são melhorias posteriores, sem compromisso na entrega atual. A pausa interrompe os perigos e a contagem dos temporizadores de jogo.

A partida mantém a fase atual somente durante a sessão. Não haverá salvamento persistente de campanha na primeira versão. Sair do aplicativo inicia uma nova campanha na próxima execução. A tela final oferece jogar novamente. Um seletor restrito de cenas pode existir na versão de desenvolvimento para a apresentação, separado da interface final.

Não há códigos de trapaça planejados. Um pequeno desenho de sabiá numa placa do posto pode funcionar como easter egg, sem requisito de coleta, conquista ou vantagem mecânica.

## Especificação dos níveis

### Estrutura comum

Cada nível contém entrada segura, dois ou três trechos de obstáculos, estação de serviço e saída. A duração de dois a três minutos é uma meta de travessia, não um limite. As medidas propostas são referências de montagem e devem mudar conforme o protótipo jogável. O cenário pode ter grande profundidade visual, mas o percurso de Nilo permanece em um plano.

Os cinco mapas reutilizam o catálogo comum de 70 assets descrito adiante e acrescentam cinco elementos característicos por cena. Essa é uma meta de produção de 75 entradas distintas utilizadas por nível, sujeita à validação do professor e à auditoria das cenas. Não significa criar 75 scripts ou mecânicas diferentes.

### Nível 1 Trilha encoberta detalhado

**Sinopse e introdução:** Nilo sai da comunidade levando o módulo. A abertura tem duas falas e mostra a direção da antena. Uma placa próxima informa A/D, Shift, Espaço e Ctrl. O trecho inicial não tem dano e permite experimentar o controle.

**Objetivo:** chegar ao posto, liberar o viveiro e ligar o sensor de passagem. A conclusão exige a estação ativada e a interação com o terminal de saída. Na build parcial, o terminal abre a tela “Nível 1 concluído”; na final, carrega Margem ferida.

**Mapa esquemático do percurso:**

Saída de Várzea Alta → área de caminhada e corrida → raiz baixa → primeiro vão → árvore marcada → passagem de deslize → tronco móvel → estação do viveiro → saída do posto.

**Trecho A, aproximadamente 0 a 20 metros:** chão largo, sem perigo, com pontos visuais para perceber a diferença entre caminhada e corrida. Um obstáculo pequeno permite saltar sem cair em estacas. O jogador aprende a parar antes de um marco. Esse trecho demonstra deslocamento e controle de posição.

**Trecho B, aproximadamente 20 a 40 metros:** primeiro vão de 1,5 a 2 metros com estacas visíveis. Há pelo menos quatro metros de aproximação e chão de pouso amplo. A marca antiga aparece antes da borda. A fala não bloqueia o comando de salto e termina antes do próximo perigo.

**Trecho C, aproximadamente 40 a 60 metros:** um galho baixo isolado ensina o deslize. Um segundo exemplo usa a mesma altura e oferece espaço para levantar. Depois, um tronco em ciclo simples pode ser observado a partir de um recuo seguro. O tutorial diz “Observe o tronco e atravesse quando a passagem abrir”.

**Trecho D, aproximadamente 60 a 80 metros:** combinação curta de salto, chão livre e deslize. O intervalo deve ser maior que o tempo de recuperação do personagem. Não combinar dois perigos que exijam decisões incompatíveis no mesmo ponto. Esse trecho encerra a prova de habilidade da primeira etapa.

**Trecho E, aproximadamente 80 a 100 metros:** área segura com viveiro, estação e caixa. E abre a caixa; a tampa produz um movimento curto e pequenos detritos recebem força e torque. O sistema escolhe um prefab de um vetor para o efeito físico secundário. A interação libera as mudas, liga o sensor e atualiza o HUD. Um sinalizador demonstra Translate e rotação sem risco para o jogador.

**Encontros:** Mara se comunica pelo rádio. Não há NPC com navegação nesta entrega. Um guarda distante ou posto da Vigília pode aparecer como elemento estático do cenário, sem ameaça ativa.

**Cinco situações de Skills:** caminhar e parar no marco; correr no trecho livre; saltar o fosso; deslizar sob o galho; observar e atravessar o tronco no momento seguro. Esses itens são situações verificáveis, ensinadas separadamente antes da combinação final.

**Elementos temáticos de apoio:** o viveiro reúne mudas, recipiente para resíduos, filtro, barreira de contenção em demonstração e faixa de solo protegido. Equipamentos de rádio, sensor, painel solar, bateria e pequeno repetidor contextualizam Tecnologia. No primeiro bimestre, Skills é o tema principal; os demais aparecem no cenário e começam a ganhar função com a estação local.

**Assets locais adicionais:** portão de Várzea Alta, marco de treinamento, árvore com marca de Nilo, caixa de ferramentas aberta e placa do viveiro. Utilizam IDs N1-01 a N1-05. O catálogo comum recebe prefixo N1 na planilha de uso da cena.

**Critérios de conclusão da etapa:** realizar toda a fase pelo teclado; acionar a estação uma única vez; visualizar o objetivo atualizado; encontrar Reiniciar após falhar; concluir a demonstração pelo terminal. Nenhum conceito obrigatório deve depender de entrar num menu oculto ou de uma explicação oral durante a apresentação.

**Como o nível 1 foi montado:** a cena é `Assets/Scenes/Nivel1_TrilhaEncoberta.unity`, com cerca de 106 unidades de comprimento e uma unidade por metro. As posições abaixo são as da cena.

| Trecho | Posição (X) | O que existe |
|---|---|---|
| A | −6 a 20 | Início em −2,5 sob o portão de Várzea Alta; fala de abertura da Mara; placa e aviso de controles; marco amarelo em 8; raiz baixa em 14 com aviso de salto |
| B | 20 a 30 | Árvore com a marca antiga em 23,5 e a fala do Nilo em 21; placa de perigo; fosso com estacas de 28 a 29,8 (1,8 de vão) |
| C | 30 a 60 | Aviso de deslize em 37; galhos baixos de 42 a 44 e de 48 a 50; recuo seguro com aviso em 52,5; tronco móvel em 57 |
| D | 60 a 75 | Segundo fosso de 64 a 65,8; galho baixo de 71 a 73, com cerca de 5 unidades de chão livre depois do pouso |
| E | 80 a 106 | Área segura do posto; estação do viveiro em 88 (caixa, portão das mudas e sensor); sinalizador no cabo entre 90,3 e 95,7; terminal de saída em 97; paredão em 106,5 |

As falas de abertura e da marca antiga seguem o roteiro deste GDD. A mensagem ao concluir a estação ("Viveiro liberado. Sensor de passagem ligado." e a resposta curta da Mara) foi escrita durante a implementação e deve ser revisada pela equipe. O tempo de travessia ainda não foi cronometrado; pelos valores de velocidade, só o deslocamento leva de 20 a 35 segundos, então a meta de 2 a 3 minutos por nível deve ser revista depois do teste com colegas.

### Nível 2 Margem ferida detalhado

**Sinopse e introdução:** um texto de rádio anuncia que o rio está próximo. A margem mostra solo exposto e uma passagem bloqueada ao fundo. O primeiro enquadramento apresenta a estação de água, estabelecendo o objetivo antes da travessia.

**Mapa:** entrada na margem → pedras de passagem → galho baixo → área de observação do bloqueio → combinação de dois saltos separados → estação de filtragem e energia solar → subida para o corredor.

**Desafio:** controlar a distância do salto e a velocidade ao pousar. O rio é parte visual do ambiente; uma região de falha abaixo do caminho substitui uma simulação de natação. Pedras e plataformas de pouso são fixas. O nível reutiliza os três tipos de perigo, sem plataforma móvel carregando o jogador.

**Objetivos da estação:** destravar o filtro local e ativar o painel solar. A interação modifica a alavanca, o estado do equipamento e a aparência do reservatório. O filtro é uma representação simplificada de uma ação de manutenção; a fase não ensina um procedimento real de tratamento de água.

**Parte crítica:** o segundo salto vem depois de uma área ampla, permitindo escolher caminhar ou correr. A câmera mostra o pouso antes da decisão. A dificuldade pode ser ajustada aproximando as plataformas, sem alterar o controlador.

**Encontros e saída:** somente rádio e ambiente. Ao ver o bloqueio, Nilo recebe a nova informação e segue pelo trecho de subida. A saída exige a estação concluída.

**Assets locais N2-01 a N2-05:** passarela quebrada de margem, comporta pequena, reservatório transparente, barco comunitário encalhado e marco de nível do rio. Tempo pretendido de dois a três minutos; 75 entradas do catálogo planejadas na cena.

### Nível 3 Corredor de caça detalhado

**Sinopse e introdução:** símbolos da Vigília e marcas antigas aparecem juntos. O rádio de Mara falha até o repetidor ser reativado. O jogador entende que a estação dará suporte à conversa que esclarece o passado de Nilo.

**Mapa:** entrada estreita → observação de um tronco → fosso curto → galho baixo → ponto de descanso → segundo ciclo de tronco → estação de resíduos e repetidor → mirante da rota alternativa.

**Desafio:** escolher o momento de avançar. O primeiro tronco tem ciclo lento; o segundo usa o mesmo comportamento com outro intervalo. Os pontos seguros ficam fora de toda a trajetória. O jogador não precisa memorizar uma sequência longa de movimentos.

**Objetivos da estação:** reunir resíduos em um recipiente e restabelecer o repetidor. A animação de interação é compartilhada com as outras cenas; a mudança visual usa objetos ativos e inativos. A conclusão libera o diálogo sobre o mapa e marca a saída como disponível.

**Parte crítica:** depois do tronco, manter chão suficiente para recuperar a postura antes do galho. O desafio não deve exigir deslizar no ar. A passagem alternativa aparece no fundo e pode receber uma marca luminosa discreta durante a fala.

**Encontros e saída:** guardas distantes fazem parte da ambientação. Uma patrulha simples de fundo só entra na versão final se o NPC principal já estiver estável. O núcleo obrigatório de NavMesh permanece no nível 4.

**Assets locais N3-01 a N3-05:** mesa com mapa da Vigília, passarela de vigilância, suporte reforçado do tronco, marco da rota alternativa e receptor direcional do repetidor. Tempo pretendido de dois a três minutos; 75 entradas planejadas.

### Nível 4 Posto das raízes detalhado

**Sinopse e introdução:** o som de ferramentas antecipa a presença de Ivo. O posto reúne os sinais de recuperação mostrados anteriormente. Uma fala explica que a encosta precisa de contenção para o trabalho continuar.

**Mapa:** chegada ao abrigo → conversa com Ivo → passagem de salto e deslize → estação de contenção e bateria → vista da oficina → subida curta → saída para a serra.

**Desafio:** combinar salto e deslize com pausas claras. O encontro com o NPC não deve interromper o ritmo por muito tempo. A ação ambiental consiste em liberar uma estrutura já preparada, representada por uma mudança de posição e estado; não há construção livre.

**Objetivos da estação:** posicionar a barreira de contenção e conectar a bateria. Um LED muda, um som confirma e Ivo caminha até o ponto de serviço. O progresso não depende de Ivo terminar seu trajeto: o evento de interação do jogador conclui o objetivo.

**NavMesh:** superfície pequena, plana ou suavemente inclinada, separada das armadilhas. Ivo percorre abrigo, bancada e barreira. Se não houver caminho válido, permanece parado com animação de trabalho; isso não deve impedir a conclusão do nível. A equipe corrige o caminho antes da entrega final.

**Parte crítica e saída:** a sequência final possui um pouso largo antes de um galho e termina em terreno livre. A vista da antena prepara o próximo capítulo. A saída usa o mesmo terminal das demais fases.

**Assets locais N4-01 a N4-05:** abrigo de Ivo, bancada de reparos, barreira articulada, carrinho de mudas e mapa comunitário atualizado. Tempo pretendido de dois a três minutos; 75 entradas planejadas.

### Nível 5 A antena detalhado

**Sinopse e introdução:** o amanhecer começa a aparecer. O rádio tem ruído, mas o objetivo da missão está visível no alto. Uma mensagem curta relembra o módulo sem repetir o tutorial.

**Mapa:** base da encosta → salto → galho baixo → descanso com vista do vale → tronco móvel → salto e deslize separados → área de regeneração → antena → encerramento.

**Desafio:** executar as ações conhecidas em uma sequência um pouco mais longa. A dificuldade deve vir da combinação, preservando as janelas já aprendidas. O último pouso leva a uma área completamente segura, onde o jogador pode ler o final.

**Objetivos da estação:** fechar uma entrada indevida na área protegida do solo e instalar o módulo na antena. Uma cerca muda de estado e o terminal recebe o módulo. A transmissão começa somente após a interação, evitando que o final seja disparado enquanto Nilo ainda está no ar.

**Parte crítica:** a combinação anterior à estação precisa manter a região de aterrissagem visível. O ponto de câmera final enquadra personagem, antena e vale sem exigir uma cinematográfica complexa. Não há chefe, contagem regressiva final ou perseguição.

**Encontros e encerramento:** respostas de rádio confirmam o alcance da transmissão. A tela final mostra a conclusão da missão, créditos dos assets e botões Jogar novamente e Menu. A recuperação do vale continua no texto final, sem exigir simular os moradores atravessando todas as fases.

**Assets locais N5-01 a N5-05:** antena principal, pedestal do módulo, mapa final de rotas, portal da área de regeneração e marcador de transmissão. Tempo pretendido de dois a três minutos; 75 entradas planejadas.

### Tutorial e treinamento

O treinamento está dentro do nível 1. Cada aviso aparece antes do primeiro uso, em local sem perigo imediato. Usar frases curtas e a tecla correspondente. O aviso de deslize deve explicar que a ação começa no chão; o de interação deve indicar o objeto e o resultado esperado.

Não haverá uma sexta fase de tutorial. Uma nova tentativa pode reapresentar os avisos essenciais. Depois que uma ação for demonstrada, os níveis seguintes usam os mesmos símbolos, evitando instruções longas. Na apresentação da equipe, a build deve ser compreensível para alguém que não conhece os protótipos do curso.

## Interface câmera e áudio

### HUD menus e ajuda

O HUD exibe nome do nível, objetivo atual e estado da estação, por exemplo “Estação 0/1”. Um aviso contextual aparece quando Nilo está próximo do objeto interativo. Não existem barras de vida, energia ou munição. A região central da tela permanece livre para observar a trajetória do salto.

O menu inicial oferece Jogar, Controles, Créditos e Sair. A pausa oferece Continuar, Reiniciar e Menu. A falha oferece Reiniciar e Menu. A conclusão parcial informa que a demonstração contém o primeiro nível; a final apresenta as respostas do rádio e os créditos. Botões usam texto legível e área de clique consistente.

Legendas têm fundo simples com contraste e nome do falante. Mensagens de ajuda não dependem somente de cor: tecla e ícone acompanham o texto. Sons de alerta também têm sinais visuais. A primeira versão não exige gravação de voz para ser compreendida.

**Como foi implementado:** a interface usa uGUI com texto da fonte padrão do Unity. O HUD mostra nome do nível, objetivo e "Estação 0/1" no canto superior esquerdo; o aviso "E — …" aparece em amarelo no centro inferior; falas e avisos de tutorial aparecem numa caixa escura no rodapé e somem sozinhos, sem pausar o jogo. O menu inicial (`Assets/Scenes/Menu.unity`) tem Jogar, Controles, Créditos e Sair. Pausa, falha e conclusão são painéis escuros sobre a fase, com os botões descritos acima; a legenda some quando um desses painéis abre. Os ícones de tecla nas mensagens ainda não existem: hoje o texto nomeia a tecla.

### Fluxo gráfico de telas

MENU → JOGAR → NÍVEL ATUAL → CONCLUSÃO

NÍVEL ATUAL → FALHA → REINICIAR → NÍVEL ATUAL

NÍVEL ATUAL ↔ PAUSA → MENU

CONCLUSÃO DOS NÍVEIS 1 A 4 → PRÓXIMO NÍVEL

CONCLUSÃO DO NÍVEL 5 → FINAL E CRÉDITOS → MENU

Na build do primeiro bimestre, a seta de conclusão do nível 1 leva à tela de demonstração concluída. A lógica usa uma configuração de entrega parcial, evitando tentar carregar cenas ausentes. Reiniciar sempre restaura a fase atual; Jogar pelo menu começa no nível 1.

### Câmera

A câmera de terceira pessoa fica lateralmente ao personagem, com perspectiva moderada ou projeção ortográfica escolhida no protótipo inicial. A proposta adota inicialmente projeção ortográfica para facilitar a leitura dos saltos. Cenário, personagem, iluminação e colisões continuam em 3D.

O acompanhamento usa deslocamento suave e limites por fase, sem controle manual de rotação. Ao avançar, o jogador precisa enxergar a borda seguinte e o local de pouso. Objetos do primeiro plano não atravessam o enquadramento principal. Tremor de câmera não é necessário; se incluído, deve ser curto e discreto.

### Som música e partículas

**Ambiente:** aves, insetos, vento e água por trecho. Sons de ocupação ficam distantes. **Personagem:** passos, salto, aterrissagem, deslize e falha. **Objetos:** madeira em movimento, clique de interação, equipamento ligando e confirmação de estação.

**Música:** uma faixa discreta ou ambiente musical simples, com volume baixo durante os diálogos. Uma mudança curta acompanha a conclusão. A licença e a origem de cada áudio devem constar nos créditos e no registro de assets.

**Partículas do primeiro bimestre:** poeira ao pousar, folhas no deslize, pedrinhas ou lascas ao abrir a caixa e um efeito curto de impacto na falha. A explosão pedida como exemplo nos tutoriais pode ser representada por uma pequena dispersão de poeira e detritos, sem violência gráfica.

**Implementado na primeira entrega:** as quatro partículas acima existem e foram configuradas pela equipe (poeira de pouso, folhas de deslize, impacto de falha e poeira da caixa, que acompanha os detritos). Os oito efeitos sonoros (salto, pouso, passos, deslize, falha, caixa, estação concluída e aviso do tronco) foram sintetizados pela equipe com o script `Ferramentas/gerar_sons.py`, sem arquivos de terceiros. Não há música nem som ambiente ainda.

**Partículas da entrega final:** a equipe cria e configura os próprios sistemas de poeira, folhas, fumaça leve e faísca do equipamento. Não basta importar um efeito pronto e descrevê-lo como autoral. Guardar capturas do Inspector e uma breve descrição da configuração para demonstrar o trabalho.

## Inteligência artificial

### Oponentes e personagens amigáveis

As armadilhas operam por ciclos e colisão, sem tomada de decisão avançada. Guardas de fundo podem permanecer estáticos. O jogo não exige perseguição, combate, visão em cone ou busca pelo jogador. Caso uma patrulha visual seja acrescentada, ela fica numa área isolada e não interfere no sucesso da missão.

Ivo é o NPC amigável obrigatório da versão final. Seu comportamento tem três estados: aguardando no abrigo, indo ao ponto de serviço e trabalhando. A interação concluída no nível 4 libera a mudança de estado. Um NavMeshAgent conduz o deslocamento em superfície preparada pela equipe.

### Navegação e detecção

O NavMesh deve cobrir apenas a área acessível a Ivo. Bancadas e paredes precisam ser consideradas no cálculo. Pontos de destino ficam sobre a superfície navegável. Verificar se o caminho é válido e se o personagem chega sem atravessar objetos. Usar uma animação de caminhada ligada à velocidade do agente.

A detecção do jogador para interação usa distância ou trigger, não NavMesh. O contato com chão e perigo pertence ao sistema de física. Separar essas responsabilidades simplifica a depuração. A navegação do NPC não controla a conclusão da fase, portanto uma falha de caminho não deve bloquear a campanha.

No primeiro bimestre, a ausência de NavMesh é deliberada no recorte de entrega; no segundo, sua implementação e demonstração são obrigatórias pelo enunciado. O relatório final deverá substituir esse planejamento pelo comportamento realmente entregue.

## Temas exigidos pelo trabalho

### Critério adotado

O enunciado permite escolher um tema no primeiro bimestre e exige os três na entrega final. Também menciona três itens em um trecho e cinco em outros. Este projeto adota o limite maior: pelo menos cinco itens identificáveis de Ecorenovação, cinco de Tecnologia e cinco de Skills no jogo final. Skills é o foco avaliativo proposto para o nível 1.

Os itens abaixo possuem identificação, localização e evidência de uso. A equipe deve mostrar esses elementos na build e no GDD atualizado. A interpretação do professor sobre o que conta como item temático precisa ser confirmada antes de fechar a produção; os exemplos de cenário não devem ser apresentados como interações já implementadas.

### Ecorenovação cinco itens

**E1 — Viveiro e mudas, nível 1:** liberar a entrada da área de cultivo. Evidência: acesso aberto, mudas protegidas e indicador da estação concluída. A função é recuperar um ponto de apoio ao replantio.

**E2 — Filtragem local, nível 2:** destravar a estação de água e mostrar o reservatório funcionando. Evidência: mudança na alavanca, indicador e estado visual do reservatório. O efeito é local e representa manutenção comunitária.

**E3 — Recolhimento de resíduos, nível 3:** agrupar material descartado em um recipiente apropriado. Evidência: objetos saem da área de passagem e aparecem no recipiente. A tarefa reutiliza a interação de estado único.

**E4 — Contenção de erosão, nível 4:** liberar uma barreira já preparada pelos moradores. Evidência: mudança da estrutura e área de trabalho protegida. Não há simulação de deslizamento de grande escala.

**E5 — Proteção do solo, nível 5:** fechar um acesso indevido à faixa de regeneração. Evidência: portão fechado, sinalização e caminho autorizado ao lado. A vegetação recuperada aparece no cenário e no encerramento, sem crescer instantaneamente durante a ação.

### Tecnologia cinco itens

**T1 — Sensor de passagem, nível 1:** equipamento que volta a indicar o estado da trilha após a estação ser ativada. O rádio de Nilo contextualiza sua utilidade, mas o item contabilizado aqui é o sensor.

**T2 — Painel solar, nível 2:** fonte de alimentação da estação local. O LED e a resposta do equipamento mudam com a interação.

**T3 — Repetidor, nível 3:** restabelece o contato de rádio e permite transmitir a rota alternativa. A fala liberada é a evidência narrativa de seu uso.

**T4 — Bateria de apoio, nível 4:** mantém o posto funcionando e acompanha o trabalho de Ivo. O estado da conexão aparece no objeto.

**T5 — Antena e módulo transmissor, nível 5:** conjunto que permite avisar todo o vale. É contado como um item funcional, sem inflar o total pelo número de peças do modelo.

### Skills cinco itens

**S1 — Controle de deslocamento:** caminhar e parar numa região segura. Apresentado no começo do nível 1 e usado em áreas de pouso estreitas no nível 2.

**S2 — Corrida controlada:** escolher a velocidade para atravessar um trecho e reduzir antes de uma borda. Usa a mesma lógica de movimento com um parâmetro de velocidade.

**S3 — Salto:** identificar a borda, acionar o comando e alcançar o pouso. A dificuldade aumenta pela composição do cenário, sem desbloquear salto duplo.

**S4 — Deslize:** reconhecer uma passagem baixa, começar no chão e sair com espaço para levantar. É a principal ação nova em relação aos protótipos consultados.

**S5 — Observação de tempo:** esperar o ciclo do tronco e atravessar a abertura segura. Não exige uma tecla nova; é uma habilidade demonstrada pelo comportamento do jogador.

Os cinco itens são situações de habilidade, e não cinco sistemas extensos de evolução do personagem. O tutorial deve diferenciá-los visualmente. Caso o professor adote outra definição de Skills, ajustar os exemplos e evidências com ele preservando o núcleo de controles.

## Arte terreno e catálogo de assets

### Direção visual e produção

A direção escolhida é 3D estilizado com geometria simples, proporções coerentes e materiais adequados à URP. Nilo tem contraste maior que o fundo. Perigos usam forma, altura e sinalização próprias. Mudas, equipamentos e estruturas comunitárias devem parecer parte do mesmo mundo, mesmo quando vierem de fontes diferentes.

Antes de importar muitos recursos, selecionar uma pequena amostra e verificar escala, materiais, rig e animações. A equipe deve criar a composição das fases, os terrenos, as marcações narrativas e as partículas finais. Modelos e animações de terceiros somente serão adotados conforme autorização do professor e condições de uso verificadas para cada recurso.

O documento não indica pacotes comerciais específicos nem declara que algum download já foi autorizado. As fontes citadas no enunciado são pontos de busca, não licenças automáticas. Registrar nome do recurso, autor, endereço de origem, licença, arquivo importado, utilização e evidência da validação do professor.

### Catálogo comum de setenta entradas

O plano prevê 70 assets distintos no catálogo comum, utilizados em cada uma das cinco cenas, distribuídos em sete categorias. Cada entrada deve corresponder a um recurso identificável. Repetir a mesma árvore dez vezes não gera dez entradas distintas. Alterar somente nome, escala ou cor de uma cópia também não será usado como justificativa para inflar a contagem.

**V01 a V10 — Vegetação:** árvore de copa larga; palmeira; árvore de tronco bifurcado; árvore jovem; tronco morto em pé; árvore com raízes expostas; bananeira; bambuzal; árvore caída ramificada; toco com raízes.

**V11 a V20 — Vegetação:** samambaia; arbusto de folhas largas; arbusto florido; moita de capim alto; moita de capim baixo; grupo de bromélias; cipó pendente; raiz de superfície; conjunto de cogumelos; folhas secas agrupadas.

**R01 a R10 — Rochas e solo:** rocha alta; laje plana; conjunto de pedras pequenas; pedra com musgo; bloco rachado; parede de ravina; barranco de terra; monte de cascalho; margem elevada; degrau natural de pedra. Esses modelos complementam o Terrain e não substituem suas camadas de textura.

**C01 a C10 — Construções:** módulo de piso de madeira; módulo de passarela; corrimão; cerca de estacas; portão simples; cobertura de palha; suporte de madeira; escada decorativa; poste de iluminação; abrigo modular. Cada módulo precisa ter uso espacial coerente e geometria reconhecível.

**T01 a T10 — Tecnologia e equipamentos:** rádio portátil; sensor de passagem; painel solar; bateria; pequeno repetidor; caixa elétrica; carretel de cabo; luminária; medidor analógico; conector de energia. Nas cenas em que não são o objetivo principal, aparecem como equipamento dos postos.

**E01 a E10 — Recuperação ambiental:** muda em recipiente; bandeja de sementes; regador; recipiente para resíduos; estrutura de filtro; saco de material de contenção; estaca de proteção; cobertura de solo; placa de área em recuperação; caixa de compostagem. Esses recursos também ajudam a tornar os temas visíveis desde o primeiro nível.

**A01 a A05 — Armadilhas e suportes:** conjunto de estacas de fosso; galho baixo com suporte; tronco móvel; estrutura de suspensão; placa de perigo. A quantidade de recursos de armadilha não altera as três famílias de comportamento previstas.

**P01 a P05 — Objetos de apoio:** caixote fechado; barril; banco; corda enrolada; cantil. Devem compor pontos de descanso e postos, com colisores apenas onde necessários.

**Total comum:** 20 de vegetação + 10 de rochas e solo + 10 de construções + 10 de tecnologia + 10 ambientais + 5 de armadilhas + 5 de apoio = 70 entradas. Personagens, sons, scripts, materiais e texturas são recursos adicionais e não entram nessa conta conservadora de objetos de cena.

### Distribuição por nível e auditoria

**Nível 1:** catálogo comum 70 + locais N1-01 a N1-05 = meta de 75. **Nível 2:** comum 70 + N2-01 a N2-05 = 75. **Nível 3:** comum 70 + N3-01 a N3-05 = 75. **Nível 4:** comum 70 + N4-01 a N4-05 = 75. **Nível 5:** comum 70 + N5-01 a N5-05 = 75.

O conjunto de recursos distintos planejados para esses inventários é 70 comuns + 25 locais = 95, reutilizados em 375 registros de uso por cena. Esse número não equivale a 375 modelos novos. O personagem e os recursos de interface continuam fora da contagem usada para cumprir o mínimo por fase.

Distribuir vegetação e rochas no fundo, estruturas nos postos e perigos no percurso. Cada asset contabilizado precisa estar efetivamente presente e identificável na cena; arquivos apenas importados no projeto não bastam para o inventário de um nível. Evitar concentrar todos os objetos no caminho jogável só para atingir quantidade.

A planilha de auditoria terá uma linha por ID e as colunas categoria, recurso, origem, licença, uso em N1, N2, N3, N4 e N5, caminho do objeto na Hierarchy e captura de evidência. Contar separadamente recursos distintos e instâncias. Antes da entrega, conferir pelo menos 70 recursos aceitos por cena e investigar qualquer item duplicado ou ausente.

O PDF não define precisamente se “assets” significa recursos distintos, instâncias ou outro critério. A contagem acima adota uma leitura conservadora. A equipe deve validar o critério com o professor no início e ajustar o catálogo se necessário. Os totais deste GDD são metas; não são uma auditoria de cenas já construídas.

**Situação em 13/10/2026:** os assets foram baixados e colocados na cena do nível 1. Ficam em `Assets/Terceiros/` (Poly Haven e OpenGameArt, todos CC0), com materiais URP próprios em `Assets/Terceiros/_Materiais`. Na cena, cada objeto leva o ID do catálogo no nome (por exemplo, `Cenario_Assets/Vegetacao/V04_Embauba`). A planilha `Docs/auditoria-assets-nivel1.csv` foi gerada a partir da cena e traz, para cada ID, o arquivo, a origem, o link, a licença, o número de usos e o caminho na Hierarchy. Pelo critério mais conservador (recursos distintos), são 73 modelos de terceiros; pelo critério de IDs, os 70 estão preenchidos, sendo a placa de perigo (A05) da equipe.

Trocas em relação ao catálogo publicado: o jacarandá (132 MB, acima do limite do GitHub) deu lugar à sumaúma; bananeira, embaúba e bambu vieram do 3TD Tropical Pack; painel solar, antena, posto da ithappy e estacas do Sketchfab exigem conta e foram trocados por câmera-sensor, lanterna de sinalização, barreira de concreto e estacas do Wood Kit 1. A árvore com a marca do Nilo (N1-03), as estacas dos fossos, os galhos baixos, o tronco móvel e a viga da estrutura agora usam modelos reais sobre os mesmos colisores; nenhum modelo de terceiros tem colisor. Modelos decorativos ficam atrás do corredor do jogador, para não esconder o Nilo. Cipó e bromélia continuam sem modelo. Ainda falta confirmar com o professor o critério de contagem e as fontes aceitas.

### Terrain e dez camadas de textura

Na entrega final, cada nível terá Terrain modelado e pintado pela equipe, integrado à composição da fase. O caminho pode utilizar colisores simples para manter o salto consistente, enquanto o Terrain forma encostas, margens, depressões e fundos. Não deixar um plano padrão como único terreno final.

As dez camadas planejadas são: L01 terra seca; L02 terra úmida; L03 lama; L04 areia de margem; L05 cascalho; L06 rocha exposta; L07 grama baixa; L08 folhas caídas; L09 solo com musgo; L10 solo em recuperação com cobertura vegetal. Escolher texturas distintas, ajustar escala e pintar transições legíveis.

Para uma aplicação conservadora do requisito, usar as dez Terrain Layers em cada terreno final, distribuídas de maneira coerente e visível. Não basta criar dez referências sem pintá-las. Manter capturas do Terrain, da lista de camadas e das áreas pintadas para a apresentação.

Água utiliza malha e material próprios; árvores e pedras podem ser objetos. Esses elementos não substituem camadas de textura do Terrain. A equipe deve identificar essas categorias corretamente no relatório, mesmo que o enunciado as cite como exemplos gerais de composição do terreno.

### Materiais iluminação e pós processamento

Padronizar materiais URP e revisar recursos que apareçam rosados ou com brilho incompatível. O contraste do personagem e das armadilhas tem prioridade sobre efeitos decorativos. Usar uma iluminação principal coerente por cena, com mudanças graduais da noite para o amanhecer.

Na entrega final, aplicar pós-processamento moderado por um Volume: ajuste de cor, exposição e, se necessário, bloom discreto nos equipamentos. Não esconder perigos com neblina ou escurecimento excessivo. Avaliar o custo dos efeitos no computador do laboratório antes de manter sombras ou partículas em alta quantidade.

## Base técnica e reaproveitamento dos estudos

### Plataforma e versão

O projeto usa Unity 6 com template Universal 3D, URP, conforme o enunciado. A versão indicada no PDF é **6000.0.8f1**. O projeto foi criado na **6000.0.80f1**, a versão 6.0 LTS instalada nas máquinas da equipe; os protótipos consultados também registram 6000.0.80f1 e 6000.5.7f1. É provável que "6000.0.8f1" no PDF seja a mesma 6000.0.80f1, mas isso ainda precisa ser confirmado abrindo o projeto no computador do laboratório antes de investir em arte e pacotes.

Não abrir e salvar os projetos de estudo numa versão mais antiga como forma de migração. Preservar os originais e transferir somente recursos autorizados e código adaptado para o novo projeto. Fixar a versão no ProjectVersion.txt e manter o manifesto e o arquivo de resolução de pacotes junto da entrega.

A linguagem é C#. A entrada usa o Input System (o projeto está configurado só com o sistema novo): os scripts leem o teclado com `Keyboard.current`, o que dispensa configurar ações no Inspector. A interface usa o `InputSystemUIInputModule`. O pacote AI Navigation já está instalado para o NavMesh da etapa final. O pacote MCP for Unity também está no projeto: é uma ferramenta de desenvolvimento usada para automatizar o Editor e não faz parte do jogo.

### O que os projetos existentes mostram

**Jogo-1-Dev-Games:** PlayerController.cs demonstra entrada, Translate e rotação; FollowPlayer.cs acompanha o jogador por deslocamento em LateUpdate. Aplicação: referência para controles, orientação e câmera. O movimento do novo personagem exige adaptação ao plano lateral e às colisões.

**jogo-2-dev-games:** PlayerController.cs, SpawnManager.cs, MoveForward.cs, DestroyOutOfBounds.cs e DetectCollisions.cs mostram instanciação, vetor de prefabs, seleção aleatória, movimento e triggers. Aplicação: efeitos de detritos, objetos secundários e limpeza de instâncias. Não será necessário reaproveitar o sistema de disparo como combate.

**jogo-3-dev-games:** PlayerController.cs demonstra salto com Rigidbody, contato por tags, animação, partículas e áudio; MoveLeft.cs, SpawnManager.cs e RepeatBackground.cs trabalham obstáculos e ambiente. Aplicação: principal referência de salto e resposta à falha. O novo jogo troca a corrida automática por movimento manual.

**jogo-5-dev-games:** GameManager.cs, Target.cs e DifficultyButton.cs mostram estados de jogo, UI, botões, reinício, corrotinas, forças e torque. Aplicação: telas, reinício da cena e movimento de detritos físicos. Não será incluído o minijogo de clicar em alvos.

A análise foi feita nos scripts e arquivos de cena; não houve execução dessas builds nesta revisão. Esses materiais demonstram o repertório de estudo e não comprovam que Rastro do Dossel esteja implementado. O deslize, as interações, a organização das cinco fases e o NPC final ainda exigem trabalho próprio.

### Componentes implementados

Todos os scripts estão em `Assets/Scripts`. Os nomes seguem o planejamento da versão 0.5, com dois acréscimos: `Interactable`, base comum da estação e do terminal, e `MainMenu`, a tela inicial.

**PlayerController:** entrada, estado físico, salto e deslize do Nilo. **PlayerFeedback:** parâmetros do Animator, sons, partículas e animações de interação e falha. **FollowCamera:** acompanhamento suave com limites. **GameManager:** estado da partida, HUD, pausa, falha, reinício e transição de cenas.

**Hazard:** contato fatal. **MovingHazard:** ciclo do tronco móvel com aviso sonoro. **Interactable:** proximidade, aviso e tecla E. **InteractableStation:** estação ambiental e tecnológica. **LevelExit:** terminal que valida o objetivo e conclui a fase. **DebrisSpawner:** vetor de prefabs, sorteio, força, torque e descarte. **TutorialTrigger:** avisos e falas por zona. **SceneryMotion:** Translate e rotação do sinalizador. **MainMenu:** painéis do menu inicial. **NpcRoute** fica para a etapa final, com o Ivo.

Prefabs: `Nilo` e os três detritos (`Detrito_Lasca`, `Detrito_Pedrinha`, `Detrito_Parafuso`) em `Assets/Prefabs`.

Tags em uso: Player, Ground, Hazard, Interactable e LevelExit. Camadas: Player, Ground (chão que habilita salto), Hazard (zonas fatais) e Debris (detritos decorativos, que não colidem com o Nilo). A camada do NPC entra na etapa final.

### Hardware desempenho e rede

O alvo obrigatório é o computador do laboratório usado na apresentação. A ficha de CPU, GPU, memória, resolução e sistema operacional deve ser registrada após o primeiro teste presencial; o enunciado não fornece essas especificações. Teclado, mouse e saída de áudio são suficientes para jogar.

Metas iniciais de desempenho: imagem estável, entrada responsiva e ausência de travamentos ao reiniciar. Buscar 60 quadros por segundo na resolução de apresentação, ajustando sombras, partículas e densidade visual se necessário. Essa é uma meta de teste, não um resultado já medido.

Não há servidor, login, multiplayer ou conexão obrigatória na partida. Rede será usada somente para colaboração, obtenção autorizada de recursos e publicação do vídeo exigido na segunda entrega. A build precisa funcionar no laboratório mesmo sem conexão.

## Documentação das funções

### Padrão de registro

O enunciado pede documentação de cada função: finalidade, argumentos de entrada, retorno e o que acontece no corpo. Cada função do projeto tem esse registro em comentário logo acima dela, no próprio script. Esta seção repete o catálogo para leitura fora do código e corresponde aos scripts da primeira entrega em 13/10/2026. Funções do ciclo de vida da Unity (Awake, Update, FixedUpdate, LateUpdate, OnTriggerEnter) e métodos privados também estão incluídos. Ao alterar um script, atualizar o comentário e esta seção juntos.

### Controle e apresentação do personagem

**PlayerController.Awake()** — Finalidade: obter componentes e guardar as dimensões originais do colisor. Entradas: nenhuma. Retorno: void. Corpo: busca Rigidbody e CapsuleCollider, trava rotação e profundidade, liga a interpolação e avisa no Console se a máscara de chão estiver vazia.

**PlayerController.Update()** — Finalidade: ler o teclado e registrar pedidos de movimento, corrida, salto e deslize. Entradas: nenhuma, usa `Keyboard.current`. Retorno: void. Corpo: lê A/D ou setas, Shift, Espaço e Ctrl/S; guarda salto e deslize como pedidos para o passo de física; define o lado para onde o Nilo olha e gira o visual na direção do movimento. Sem controle, zera a entrada.

**PlayerController.FixedUpdate()** — Finalidade: atualizar a movimentação física. Entradas: nenhuma. Retorno: void. Corpo: consulta o chão, processa o pedido de deslize, define a velocidade horizontal do Rigidbody e consome o pedido de salto dentro da tolerância. Não há física no Update.

**PlayerController.CheckGround()** — Finalidade: verificar apoio válido sob os pés. Entradas: nenhuma; usa posição, raio da cápsula e `groundMask`. Retorno: bool. Corpo: lança uma esfera curta para baixo e aceita só superfícies quase horizontais; enquanto o Nilo sobe, devolve falso.

**PlayerController.TryJump()** — Finalidade: iniciar um salto quando permitido. Entradas: nenhuma. Retorno: bool indicando se o salto aconteceu. Corpo: recusa sem controle, abaixado ou fora da tolerância de apoio; zera a velocidade vertical e aplica `AddForce` com `VelocityChange`.

**PlayerController.TrySlide()** — Finalidade: iniciar o deslize. Entradas: nenhuma. Retorno: bool. Corpo: exige chão e que o Nilo não esteja abaixado; reduz a cápsula e inicia o temporizador. Repetir o comando não prolonga a ação.

**PlayerController.UpdateSlide(float deltaTime)** — Finalidade: controlar a duração e a saída do deslize. Entrada: tempo do passo de física. Retorno: void. Corpo: reduz o tempo restante e só restaura a cápsula em pé se houver espaço acima.

**PlayerController.HasHeadroom()** (privado) — Finalidade: saber se a cápsula em pé caberia na posição atual. Entradas: nenhuma. Retorno: bool. Corpo: testa a cápsula original contra `headroomMask`, ignorando a camada do Nilo e triggers.

**PlayerController.SetCrouched(bool crouched)** (privado) — Finalidade: alternar entre cápsula em pé e abaixada. Entrada: postura desejada. Retorno: void. Corpo: ajusta altura e centro mantendo a base nos pés e achata o visual na mesma proporção.

**PlayerFeedback.Awake()** — Finalidade: obter o controlador e o Rigidbody do Nilo. Entradas: nenhuma. Retorno: void.

**PlayerFeedback.Update()** — Finalidade: atualizar animação e efeitos a partir do estado do Nilo. Entradas: nenhuma. Retorno: void. Corpo: envia velocidade, chão, postura, velocidade vertical e interação ao Animator; toca som e partícula só na mudança de estado (salto, pouso, início do deslize); toca passos em intervalos que dependem da velocidade.

**PlayerFeedback.PlayInteract()** — Finalidade: tocar a animação de interação. Entradas: nenhuma. Retorno: void. Corpo: liga o parâmetro de interação pelo tempo do clipe; ignorado depois da falha.

**PlayerFeedback.PlayFail()** — Finalidade: resposta de falha do Nilo. Entradas: nenhuma. Retorno: void. Corpo: executa uma única vez: som, partícula de impacto e animação de falha.

**PlayerFeedback.Play(AudioClip clip, float volume)** (privado) — Finalidade: tocar um efeito curto. Entradas: clipe e volume. Retorno: void. Corpo: ignora clipe ou fonte ausentes e usa `PlayOneShot`.

**FollowCamera.LateUpdate()** — Finalidade: acompanhar o Nilo depois que ele se moveu. Entradas: nenhuma. Retorno: void. Corpo: calcula o alvo com deslocamento à frente, limita X à área da fase e aplica `SmoothDamp`.

### Regras de fase, interface e objetos

**GameManager.Awake()** — Finalidade: preparar a fase ao carregar a cena. Entradas: nenhuma. Retorno: void. Corpo: registra a instância, restaura o tempo, esconde painéis e preenche o HUD.

**GameManager.Update()** — Finalidade: ler R e Esc e expirar a mensagem atual. Entradas: nenhuma. Retorno: void. Corpo: R reinicia em qualquer estado; Esc alterna a pausa só entre jogando e pausado.

**GameManager.StartLevel(int sceneIndex)** (estático) — Finalidade: iniciar uma cena válida. Entrada: índice na lista de cenas da build. Retorno: void. Corpo: recusa índices fora da build, restaura o tempo e carrega a cena.

**GameManager.FailLevel()** — Finalidade: encerrar a tentativa. Entradas: nenhuma. Retorno: void. Corpo: ignora repetições, bloqueia o controle, congela o Nilo, aciona a resposta de falha uma vez, esconde a legenda e abre o painel com Reiniciar.

**GameManager.RestartLevel()** — Finalidade: repetir a fase. Entradas: nenhuma. Retorno: void. Corpo: recarrega a cena ativa. Botões Reiniciar e a tecla R usam este método.

**GameManager.SetPaused(bool paused)** — Finalidade: alterar a pausa. Entrada: estado desejado. Retorno: void. Corpo: só pausa jogando e só retoma pausado; ajusta escala de tempo, controle e painel.

**GameManager.Resume(), GoToMenu(), PlayFromStart()** — Finalidade: ações dos botões Continuar, Menu e Jogar novamente. Entradas: nenhuma. Retorno: void. Corpo: chamam `SetPaused(false)`, `StartLevel(0)` e `StartLevel(1)`.

**GameManager.CompleteLevel()** — Finalidade: concluir a fase. Entradas: nenhuma. Retorno: void. Corpo: bloqueia repetições; na entrega parcial (`partialDelivery`) ou sem próxima cena na build, mostra a tela de conclusão; senão carrega o nível seguinte.

**GameManager.StationCompleted(string nextObjective)** — Finalidade: registrar uma estação concluída. Entrada: texto do objetivo seguinte. Retorno: void. Corpo: soma uma estação e atualiza contador e objetivo.

**GameManager.SetObjective(string text), ShowMessage(string text, float duration), SetPrompt(string text), ClearPrompt(string text)** — Finalidade: escrever no HUD. Entradas: texto e, na mensagem, segundos visíveis. Retorno: void. Corpo: `ClearPrompt` só apaga o aviso se ele ainda for do objeto que pediu.

**GameManager.UpdateStationText()** (privado) — Finalidade: atualizar "Estação x/y". Entradas: nenhuma. Retorno: void.

**Hazard.OnTriggerEnter(Collider other)** — Finalidade: detectar contato fatal. Entrada: colisor que entrou. Retorno: void. Corpo: se for o Nilo, pede `FailLevel`.

**MovingHazard.Awake()** — Finalidade: preparar o Rigidbody cinemático e o ponto inicial. Entradas: nenhuma. Retorno: void.

**MovingHazard.FixedUpdate()** — Finalidade: mover o tronco pelo ciclo fixo. Entradas: nenhuma. Retorno: void. Corpo: avança o tempo próprio (para na pausa, zera ao recarregar), calcula a fração do percurso, toca o aviso sonoro ao sair do ponto inicial e move com `MovePosition`.

**Interactable.Update()** — Finalidade: detectar proximidade e a tecla E. Entradas: nenhuma. Retorno: void. Corpo: com a partida em andamento, mostra ou apaga o aviso quando a proximidade muda e chama `Interact`; se houve efeito, toca a animação de interação do Nilo.

**Interactable.Interact()** (abstrato) e **Available** (propriedade) — Finalidade: ação do objeto e se ele ainda aceita interação. Retorno: bool.

**InteractableStation.Interact()** — Finalidade: concluir a estação. Entradas: nenhuma. Retorno: bool. Corpo: recusa se já concluída; troca os objetos visuais, dispara o evento `onCompleted`, atualiza o objetivo e mostra a mensagem.

**LevelExit.Interact()** — Finalidade: validar a saída. Entradas: nenhuma. Retorno: bool. Corpo: com estação pendente, mostra o aviso e não avança; senão chama `CompleteLevel`.

**TutorialTrigger.OnTriggerEnter(Collider other)** — Finalidade: mostrar ajuda ou fala no local certo. Entrada: colisor que entrou. Retorno: void. Corpo: só para o Nilo e uma vez por tentativa.

**DebrisSpawner.Burst()** — Finalidade: disparar os detritos a partir do próprio objeto. Entradas: nenhuma. Retorno: void. Corpo: chama `SpawnDebris` com a posição e a quantidade configuradas.

**DebrisSpawner.SpawnDebris(Vector3 origin, int count)** — Finalidade: criar detritos visuais da caixa. Entradas: posição e quantidade. Retorno: void. Corpo: valida o vetor, toca poeira e som; para cada detrito sorteia um índice válido, instancia o prefab, aplica `AddForce` e `AddTorque` e agenda o descarte em 4 segundos.

**SceneryMotion.Update()** — Finalidade: demonstrar `transform.Translate` e rotação. Entradas: nenhuma. Retorno: void. Corpo: move o sinalizador até a distância configurada, inverte o sentido nas pontas e gira a hélice.

**MainMenu.Awake(), Play(), ShowControls(), ShowCredits(), Back(), Quit()** — Finalidade: painéis e botões do menu inicial. Entradas: nenhuma. Retorno: void. Corpo: `Play` chama `GameManager.StartLevel(1)`; `Quit` fecha o aplicativo (sem efeito no Editor).

**MainMenu.ShowPanel(GameObject panel)** (privado) — Finalidade: deixar visível só um painel. Entrada: painel. Retorno: void.

### Navegação (etapa final)

**NpcRoute.SetDestination(int pointIndex)** e **NpcRoute.Update()** continuam planejados para o Ivo no nível 4, com o contrato descrito na versão 0.5: validar o índice e o NavMesh, acompanhar a chegada e nunca bloquear o jogador quando o caminho falhar.

## Plano de execução e entregas

### Primeiro bimestre

**Marco A — Projeto e personagem:** criar o projeto URP na versão exigida, preparar uma cena de teste e validar caminhada, corrida, salto, deslize e câmera. Fazer um teste inicial no laboratório. Critério de avanço: atravessar obstáculos simples sem comportamento inconsistente do colisor.

*Situação:* concluído no Editor em 9/10/2026, com a cena `TestePersonagem`. O teste no laboratório ainda não foi feito.

**Marco B — Nível 1 completo em formas simples:** montar entrada, tutorial, perigos, estação e saída. Criar os estados de menu, pausa, falha e conclusão parcial. Critério: conseguir iniciar, falhar, reiniciar e concluir sem usar o Inspector para corrigir o estado.

*Situação:* concluído em 9/10/2026. O fluxo menu → fase → falha → reiniciar → estação → saída → conclusão funcionou pelos próprios botões.

**Marco C — Conceitos e apresentação:** adicionar o vetor de prefabs, sorteio, spawn por função, força, torque, rotação, Translate secundário, tags, animações, sons e partículas. Montar o catálogo de assets do nível e conferir as evidências de Skills. Documentar todas as funções implementadas.

*Situação:* código concluído em 9/10/2026, incluindo a animação de interação e o novo visual do Nilo. Catálogo pronto em `Docs/catalogo-assets.html`. Falta importar os assets.

**Marco D — Entrega parcial:** revisar materiais, legibilidade e desempenho no laboratório. Atualizar o GDD com o que realmente funciona. Preparar o projeto compactado e apresentar somente o nível 1. Datas exatas dependem do calendário fornecido pelo professor; este documento não inventa prazos.

*Situação:* em andamento. GDD atualizado nesta versão e pacote compactado gerado em 13/10/2026. Faltam os assets, o teste no laboratório e a revisão de desempenho.

### Segundo bimestre

**Marco E — Expansão para cinco níveis:** duplicar a estrutura funcional de cena e compor os quatro novos percursos. Ajustar estações e diálogos por dados, sem copiar scripts inteiros com alterações mínimas. Conferir transições e encerramento.

**Marco F — Requisitos finais:** concluir os quinze itens temáticos, modelar e pintar os terrenos com dez camadas, implementar Ivo e NavMesh, melhorar animações, criar partículas próprias, revisar iluminação, materiais e pós-processamento.

**Marco G — Verificação e entrega final:** auditar os assets das cinco cenas, documentar todas as funções, executar a campanha inteira no laboratório, atualizar o GDD e gravar o vídeo de um a três minutos. Enviar o link em Google Drive ou OneDrive conforme o enunciado e conferir o acesso de quem avaliará.

### Organização da equipe e controle de escopo

Distribuir responsabilidades por personagem e física; fases e terreno; interface e integração; arte, áudio e documentação. Uma pessoa pode assumir mais de uma frente conforme o tamanho real da equipe. A equipe é formada por Matheus Braga, Moruan Mattar, Nícolas Muciaccia e Nícolas Ramos. A divisão de frentes por integrante ainda será registrada pela equipe; esta versão não presume quem fez cada parte.

Registrar tarefas, autoria, revisões e testes por integrante, pois o enunciado informa avaliação proporcional à participação. O registro pode ser uma planilha simples com data, tarefa, responsável e evidência, acompanhado do histórico do repositório. Não atribuir uma tarefa a alguém sem combinar a divisão.

Se o prazo apertar, reduzir duração das fases, quantidade de falas e variedade de animações opcionais. Preservar cinco níveis, os mínimos de assets, os temas, os conceitos dos tutoriais e os requisitos específicos da entrega final. O catálogo compartilhado é a principal forma de reduzir produção de arte sem diminuir a quantidade mínima em cada cena.

## Verificação de atendimento ao enunciado

### Requisitos gerais e primeira etapa

| Requisito | Situação | Evidência no projeto |
|---|---|---|
| Unity 6 e Universal 3D (URP) | Atendido, versão a confirmar | `ProjectSettings/ProjectVersion.txt` (6000.0.80f1); confirmar no laboratório se corresponde à 6000.0.8f1 do PDF |
| Cinco níveis no planejamento, só o nível 1 na primeira entrega | Atendido | Build com `Menu` (0) e `Nivel1_TrilhaEncoberta` (1); `partialDelivery` ligado no GameManager |
| Mínimo de 70 assets por nível | Atendido no nível 1, critério a confirmar | `Docs/auditoria-assets-nivel1.csv`: 70 IDs preenchidos (69 de terceiros e 1 da equipe), 73 modelos distintos e 118 usos na cena, todos visíveis |
| Colisão e movimento | Atendido | Chão, fossos, galhos e paredes; `PlayerController` |
| `transform.Translate` | Atendido | Sinalizador do posto (`SceneryMotion`) |
| `AddForce` | Atendido | Salto do Nilo e detritos da caixa |
| Rotação e `AddTorque` | Atendido | Giro do visual do Nilo, hélice do sinalizador e torque nos detritos |
| Vetor de prefabs, sorteio e spawn por função | Atendido | `DebrisSpawner.SpawnDebris` sorteia entre três prefabs ao concluir a estação |
| Tags | Atendido | Player, Ground, Hazard, Interactable e LevelExit |
| Scripts, animações, partículas e som | Atendido | 13 scripts; 8 animações; 4 sistemas de partículas; 8 efeitos sonoros |
| UI de texto e botão Reiniciar | Atendido | HUD, legendas, pausa e painel de falha com Reiniciar (e tecla R) |
| Tema do primeiro bimestre (Skills) | Atendido | Cinco situações S1–S5 com avisos no nível 1 |
| GDD detalhado e funções documentadas | Atendido nesta versão | Este documento e os comentários em cada script |
| Execução no computador da aula | Pendente | Teste presencial ainda não feito |

### Requisitos específicos da etapa final

**Ecorenovação, Tecnologia e Skills:** cinco itens de cada, identificados por E1–E5, T1–T5 e S1–S5. Evidência: estações, situações jogáveis, estados visuais e falas previstas. Adota-se cinco para satisfazer o maior mínimo presente no documento.

**Terrain próprio com dez camadas de textura:** modelagem e pintura pela equipe, conforme L01–L10. **Materiais com cor e textura:** recursos revisados para URP. Evidência: capturas do editor, terrenos presentes nas cenas e execução.

**NavMesh e NPC:** Ivo no posto do nível 4. **Melhores animações:** transições e velocidade ajustadas ao movimento. **Partículas criadas pela equipe:** efeitos configurados e registrados. **Iluminação e pós-processamento:** cenas com iluminação planejada e Volume moderado.

**Qualidade de gameplay e mecânica:** repetibilidade dos comandos, leitura do perigo, falhas compreensíveis e progressão curta. **Qualidade gráfica:** coerência de materiais, cenário sem oclusões indevidas e terrenos trabalhados. Esses itens exigem avaliação da build; o texto do GDD não comprova qualidade por si só.

### Testes de aceitação

Os testes abaixo foram executados no Editor em 9/10/2026, na máquina de Matheus Braga, com entrada de teclado simulada e chamadas diretas aos métodos. Eles verificam a lógica; não substituem uma pessoa jogando a fase inteira com teclado real, que continua pendente.

| Teste | Resultado |
|---|---|
| Caminhar | 3,00 unidades por segundo, pés no chão |
| Encostar num obstáculo baixo | Parou na borda, sem subir sozinho |
| Saltar correndo sobre a raiz e o vão de 1,8 | Passou |
| Manter Espaço pressionado | Um salto só |
| Sair do fosso de 1 unidade | Passou |
| Encostar de lado num bloco em pleno ar | Não contou como chão |
| Deslize | 2,97 unidades de distância |
| Deslize terminando sob o galho | Ficou abaixado, recusou o salto e saiu andando |
| Cair no fosso | Painel de falha; segunda chamada sem efeito duplicado |
| Reiniciar pelo botão | Posição, estação 0/1 e tempo normal restaurados |
| Pausar e continuar | Tempo parado e retomado, controle bloqueado e devolvido |
| Saída antes da estação | Mostrou a pendência e não avançou |
| Estação acionada duas vezes, com E | Progresso 1/1, sem duplicar; detritos, poeira, som e animação de interação |
| Saída depois da estação | Tela de conclusão; Jogar novamente recarregou o nível |
| Tronco descendo sobre o Nilo | Falha |
| Animações | Locomoção com braços e pernas, queda, deslize, interação e falha observados |

Um defeito foi encontrado e corrigido durante os testes: um objeto saindo do alcance apagava o aviso de outro que acabava de entrar. Em um teste com teletransporte, o Nilo subiu cerca de 1,3 unidade sem comando; o efeito não se repetiu com teclado e provavelmente vem de mover o Rigidbody por código. Fica registrado para observação no teste com teclado real.

**Ainda pendentes:** jogar a fase inteira com teclado real; pausar durante o tronco em movimento; voltar ao menu e iniciar de novo; teste com colegas sem explicação oral; cronometrar a travessia; teste no computador do laboratório.

### Pacote de entrega e apresentação

Preparar um arquivo compactado com o projeto Unity e os diretórios exigidos, usando seus nomes reais: Assets, ProjectSettings, UserSettings e Packages. Os scripts dentro de Assets e os arquivos .meta precisam acompanhar o projeto. Incluir também os arquivos de configuração necessários para reabertura. Seguir qualquer orientação adicional do professor sobre compactar o projeto inteiro.

Guardar o GDD atualizado e o registro de recursos usados. Extrair uma cópia do pacote em outro diretório e abri-la na versão do laboratório para conferir referências, materiais, cenas e configuração. A apresentação presencial exige que o jogo execute no computador da aula, além da existência dos arquivos.

No segundo bimestre, o vídeo de um a três minutos deve mostrar o objetivo, o controle do personagem, exemplos de fases, temas e os recursos finais exigidos. Uma sequência sugerida é: abertura breve; nível 1 e movimentos; estações dos níveis seguintes; Ivo e terreno; antena e créditos. O vídeo complementa a execução presencial.

**Pacote da primeira entrega:** `RastroDoDossel-Entrega1.zip`, gerado em 13/10/2026, com Assets, Packages, ProjectSettings, UserSettings e a pasta Docs. Library, Logs e arquivos de IDE ficam de fora, porque o Unity os recria ao abrir. O pacote MCP for Unity, ferramenta de desenvolvimento baixada por git, foi retirado do `manifest.json` da cópia compactada para que o projeto abra no laboratório sem git e sem esse download.

A entrega acadêmica depende da implementação, da validação dos recursos de terceiros e da participação efetiva do grupo.

## Fontes e histórico desta revisão

**GDD_Template.Rev01.docx, fornecido pela disciplina:** referência de estrutura e apresentação. Seus tópicos de título, visão, história, lentes, mundo, personagens, jogabilidade, mecânicas, níveis, interface, IA, aspectos técnicos e arte estão preenchidos nesta adaptação. A história foi posicionada antes da visão por solicitação da equipe.

**Trabalho_2026-2.pdf, páginas 1 a 3:** fonte das exigências de versão, URP, entregas, conceitos dos tutoriais, temas, níveis, assets, documentação, Terrain, NavMesh, NPC, efeitos e apresentação. As divergências de quantidade temática e a ambiguidade de contagem de assets foram tratadas explicitamente nas seções correspondentes.

**Projetos da pasta dev-games-unityhub:** Jogo-1-Dev-Games, jogo-2-dev-games, jogo-3-dev-games e jogo-5-dev-games, consultados por scripts e arquivos de projeto. Servem de referência de técnicas estudadas, sem afirmar validação de execução.

**Alterações da versão 0.5:** expansão de três para cinco níveis; foco do primeiro bimestre no nível 1; inclusão de Ecorenovação, Tecnologia e Skills com cinco itens cada; catálogo de assets; plano de Terrain, NPC e NavMesh; contratos de funções; fluxo de telas; critérios de aceitação e divisão das entregas. História de Nilo, ambientação na selva e foco em mover, pular e deslizar foram preservados.

**Alterações da versão 0.6:** assets do nível 1 baixados, colocados na cena e auditados; registro do que foi implementado e testado na primeira entrega; nomes dos integrantes; quadro de situação no início; valores físicos reais e as tolerâncias de salto; tronco móvel como pilão vertical; zona de queda geral; visual de guerrilheiro da selva e animações do Nilo; mapa do nível 1 com posições da cena; interface, sons sintetizados e partículas; componentes e documentação das funções conforme o código; situação dos marcos, quadro de requisitos e resultados dos testes; situação do catálogo de assets e do pacote de entrega.
