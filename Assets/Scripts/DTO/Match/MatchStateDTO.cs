using System.Collections.Generic;

/// <summary>
/// A partida do ponto de vista de um jogador, como o servidor manda em
/// `match_start` (Match.get_state_for_player).
///
/// As colecoes nascem vazias de proposito: enquanto o servidor ainda mandar
/// payload vazio, o estado chega vazio em vez de nulo, e quem consome nao
/// precisa checar null em todo lugar.
///
/// `board` vem com chave string do JSON -- chave de objeto JSON sempre e --
/// e o Newtonsoft converte de volta para int. O JsonUtility nao daria conta:
/// ele nao desserializa dicionario nenhum.
/// </summary>
public class MatchStateDTO
{
    public List<string> your_hand = new();
    public Dictionary<int, List<string>> board = new();
    public int turn;
}
