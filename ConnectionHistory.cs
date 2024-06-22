using System;
using System.Collections.Generic;

public class ConnectionHistory
{
  public int fromNode;
  public int toNode;
  public int innovationNumber;
  public List<int> innovationNumbers;

  public ConnectionHistory(int fromNode, int toNode, int innovationNum, List<int> innovationNumbers)
  {
    this.fromNode = fromNode;
    this.toNode = toNode;
    this.innovationNumber = innovationNum;
    this.innovationNumbers = new List<int>(innovationNumbers);
  }

  // Returns whether the genome matches the original genome and the connection is between the same nodes
  public bool Matches(Genome genome, Node fromNode, Node toNode)
  {
    if (genome.connections.Count == this.innovationNumbers.Count)
    {
      if (fromNode.id == this.fromNode && toNode.id == this.toNode)
      {
        // Check if all the innovation numbers match from the genome
        foreach (var connection in genome.connections)
        {
          if (!this.innovationNumbers.Contains(connection.innovationNumber))
          {
            return false;
          }
        }

        // If reached this far then the innovation numbers match the connections' innovation numbers and the connection is between the same nodes, so it does match
        return true;
      }
    }
    return false;
  }
}
