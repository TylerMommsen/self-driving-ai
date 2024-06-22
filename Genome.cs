using System.Collections.Generic;
using UnityEngine;

public class Genome
{
  public List<Connection> connections = new List<Connection>(); // a list of connections which represent the network
  public List<Node> nodes = new List<Node>(); // a list of nodes which also represent the network

  public int inputs;
  public int outputs;
  public int layers = 2;
  public int nextNode = 0;
  public int biasNode;

  public List<Node> network = new List<Node>(); // a list of the nodes in the order that they need to be considered in the NN

  public Genome(int inputs, int outputs, bool crossover = false)
  {
    this.inputs = inputs;
    this.outputs = outputs;

    if (!crossover)
    {
      CreateNodes();
    }
  }

  // create the nodes
  private void CreateNodes()
  {
    // create input nodes
    for (int i = 0; i < this.inputs; i++)
    {
      this.nodes.Add(new Node(i));
      this.nextNode++;
      this.nodes[i].layer = 0;
    }

    // create output nodes
    for (int i = 0; i < this.outputs; i++)
    {
      this.nodes.Add(new Node(i + this.inputs));
      this.nodes[i + this.inputs].layer = 1;
      this.nextNode++;
    }

    // create bias node
    this.nodes.Add(new Node(this.nextNode));
    this.biasNode = this.nextNode;
    this.nextNode++;
    this.nodes[this.biasNode].layer = 0;
  }

  // pretty simple
  public Node GetNode(int id)
  {
    foreach (var node in this.nodes)
    {
      if (node.id == id)
      {
        return node;
      }
    }
    return null;
  }

  // adds the connections going out of a node to that node so that it can access the next node during feeding forward
  public void ConnectNodes()
  {
    foreach (var node in this.nodes)
    {
      node.outputConnections = new List<Connection>();
    }

    foreach (var connection in this.connections)
    {
      connection.fromNode.outputConnections.Add(connection);
    }
  }

  // process inputs
  public List<float> FeedForward(List<float> vision)
  {
    // set output values of the input nodes
    for (int i = 0; i < this.inputs; i++)
    {
      this.nodes[i].outputValue = vision[i];
    }

    // set output value of bias node
    this.nodes[biasNode].outputValue = 1;

    // engage each node in the network sequentially
    foreach (var node in this.network)
    {
      node.Engage();
    }

    // collect the outputs from the output nodes
    List<float> outputValues = new List<float>();
    for (int i = 0; i < this.outputs; i++)
    {
      outputValues.Add(this.nodes[this.inputs + i].outputValue);
    }

    // reset all the input values of all nodes so that no leftover values affect the next calculations
    foreach (var node in this.nodes)
    {
      node.inputValue = 0;
    }

    return outputValues;
  }

  // sets up the full NN as a list of nodes in order
  public void GenerateNetwork()
  {
    ConnectNodes();
    network.Clear();

    for (int j = 0; j < this.layers; j++)
    {
      foreach (var node in this.nodes)
      {
        // if that node is in that layer
        if (node.layer == j)
        {
          network.Add(node);
        }
      }
    }
  }

  // mutates the network by adding a new node by disabling a random connection and connecting the new node in its place.
  public void AddNode(List<ConnectionHistory> innovationHistory)
  {
    // ensure there is at least one connection to mutate
    if (this.connections.Count == 0)
    {
      AddConnection(innovationHistory);
      return;
    }

    // select a valid connection that does not involve the bias node unless it is the only connection
    int randomConnection;
    do
    {
      randomConnection = Random.Range(0, this.connections.Count);
    } while (
      this.connections[randomConnection].fromNode.id == this.nodes[biasNode].id &&
      this.connections.Count != 1
    );

    // disable the selected connection
    Connection connection = this.connections[randomConnection];
    connection.enabled = false;

    // create a new node and adjust the node number
    Node newNode = new Node(this.nextNode++);
    this.nodes.Add(newNode);
    newNode.layer = connection.fromNode.layer + 1;

    // add new connections to and from the new node
    this.connections.Add(
      new Connection(
        connection.fromNode,
        newNode,
        1,
        GetInnovationNumber(innovationHistory, connection.fromNode, newNode)
      )
    );
    this.connections.Add(
      new Connection(
        newNode,
        connection.toNode,
        connection.weight,
        GetInnovationNumber(innovationHistory, newNode, connection.toNode)
      )
    );

    // connect the bias node to the new node with a weight of 0
    this.connections.Add(
      new Connection(
        this.nodes[this.biasNode],
        newNode,
        0,
        GetInnovationNumber(innovationHistory, this.nodes[biasNode], newNode)
      )
    );

    // adjust layers if necessary
    if (newNode.layer == connection.toNode.layer)
    {
      foreach (var node in this.nodes)
      {
        if (node.layer >= newNode.layer && node != newNode)
        {
          node.layer++;
        }
      }
      this.layers++;
    }

    // reconnect nodes to update connections
    ConnectNodes();
  }

  // adds a connection between 2 nodes which aren't already connected
  public void AddConnection(List<ConnectionHistory> innovationHistory)
  {
    if (FullyConnected())
    {
      return;
    }

    // get a random node
    int randomNode1 = Random.Range(0, this.nodes.Count);
    int randomNode2 = Random.Range(0, this.nodes.Count);

    // make sure the nodes aren't in the same layer or are already connected
    while (RandomConnectionNodesFailed(randomNode1, randomNode2))
    {
      randomNode1 = Random.Range(0, this.nodes.Count);
      randomNode2 = Random.Range(0, this.nodes.Count);
    }

    // if the first random node is after the second then switch the nodes
    // this is because you want the connection to go from node1 TO node2
    int temp;
    if (nodes[randomNode1].layer > this.nodes[randomNode2].layer)
    {
      temp = randomNode2;
      randomNode2 = randomNode1;
      randomNode1 = temp;
    }

    // get innovation number of the connection
    // this will be a new number if no identical genome has mutated in the same way
    int connectionInnovationNumber = GetInnovationNumber(
      innovationHistory,
      this.nodes[randomNode1],
      this.nodes[randomNode2]
    );

    // add the connection with a random weight
    this.connections.Add(
      new Connection(
        this.nodes[randomNode1],
        this.nodes[randomNode2],
        Random.Range(-1f, 1f),
        connectionInnovationNumber
      )
    );
    ConnectNodes();
  }

  // checks if 2 nodes are in the same layer or are already connected
  private bool RandomConnectionNodesFailed(int r1, int r2)
  {
    if (this.nodes[r1].layer == this.nodes[r2].layer) return true; // if the nodes are in the same layer
    if (this.nodes[r1].IsConnectedTo(this.nodes[r2])) return true; // if the nodes are already connected
    return false;
  }

  // returns the innovation number for the new mutation
  // if this mutation has never been seen before then it will be given a new unique innovation number
  // if this mutation matches a previous mutation then it will be given the same innovation number as the previous one
  private int GetInnovationNumber(List<ConnectionHistory> innovationHistory, Node fromNode, Node toNode)
  {
    bool isNew = true;
    int connectionInnovationNumber = InnovationManager.Instance.nextConnectionNumber;

    foreach (var history in innovationHistory)
    {
      if (history.Matches(this, fromNode, toNode))
      {
        isNew = false; // it's not a new mutation
        connectionInnovationNumber = history.innovationNumber;
        break;
      }
    }

    // if the mutation is new then create a list representing the current state of the genome
    if (isNew)
    {
      List<int> innovationNumbers = new List<int>();

      foreach (var connection in this.connections)
      {
        innovationNumbers.Add(connection.innovationNumber);
      }

      // add this mutation to the innovationHistory
      innovationHistory.Add(
        new ConnectionHistory(fromNode.id, toNode.id, connectionInnovationNumber, innovationNumbers)
      );
      InnovationManager.Instance.nextConnectionNumber++;
    }
    return connectionInnovationNumber;
  }

  // returns whether a network is fully connected or not
  private bool FullyConnected()
  {
    int maxConnections = 0;
    List<int> nodesInLayers = new List<int>();
    for (int i = 0; i < this.layers; i++)
    {
      nodesInLayers.Add(0);
    }

    // populate array
    for (int i = 0; i < this.nodes.Count; i++)
    {
      if (i >= 0 && i < this.nodes.Count) {
        nodesInLayers[this.nodes[i].layer] += 1;
      } else {
        Debug.Log($"nodesInLayers index is out of bounds: {i}");
      }
    }

    // for each layer the maximum amount of connections is the number in the layer * the numbers of nodes in front of it
    // so let's add the max for each layer together and then we will get the maximum amount of connections in the network
    for (int i = 0; i < this.layers - 1; i++)
    {
      int nodesInFront = 0;
      for (int j = i + 1; j < this.layers; j++)
      {
        if (j >= 0 && j < this.layers) {
          nodesInFront += nodesInLayers[j]; // add up nodes
        } else {
          Debug.Log($"nodesInFront index is out of bounds: {i}");
        }
      }

      maxConnections += nodesInLayers[i] * nodesInFront;
    }

    if (maxConnections <= this.connections.Count)
    {
      return true;
    }

    return false;
  }

  public void CreateInitialConnections(List<ConnectionHistory> innovationHistory) {
    if (FullyConnected()) {
      return;
    }

    // Assume the last 'outputs' nodes are output nodes, excluding the bias node
    int outputsStartIndex = nodes.Count - 1 - outputs; // Index where output nodes start

    for (int i = 0; i < inputs; i++) {
      Node inputNode = nodes[i];

      // Loop through each output node
      for (int j = outputsStartIndex; j < nodes.Count - 1; j++) { // Exclude the bias node at the very end
        Node outputNode = nodes[j];

        // Get the innovation number for the new connection
        int connectionInnovationNumber = GetInnovationNumber(innovationHistory, inputNode, outputNode);

        // Add the connection with a random weight
        connections.Add(new Connection(
            inputNode,
            outputNode,
            Random.Range(-1f, 1f),
            connectionInnovationNumber
        ));
      }
    }

    // connect the bias node
    // Node biasNode = nodes[nodes.Count - 1];
    // for (int j = outputsStartIndex; j < nodes.Count - 1; j++) {
    //   Node outputNode = nodes[j];
    //   int connectionInnovationNumber = GetInnovationNumber(innovationHistory, biasNode, outputNode);
    //   connections.Add(new Connection(
    //     biasNode,
    //     outputNode,
    //     Random.Range(-1f, 1f),
    //     connectionInnovationNumber
    //   ));
    // }

    ConnectNodes();  // This should ideally be called once after all connections are added
  }

  // mutates the genome/brain
  public void Mutate(List<ConnectionHistory> innovationHistory)
  {
    if (this.connections.Count == 0)
    {
      AddConnection(innovationHistory);
    }

    // 80% of the time mutate the weights
    if (Random.Range(0f, 1f) < 0.8f)
    {
      foreach (var connection in this.connections)
      {
        connection.MutateWeight();
      }
    }

    // 5% of the time add a new connection
    if (Random.Range(0f, 1f) < 0.05f)
    {
      AddConnection(innovationHistory);
    }

    // // 1% of the time add a new node
    if (Random.Range(0f, 1f) < 0.01f)
    {
      AddNode(innovationHistory);
    }
  }

  public Genome Crossover(Genome parent2)
  {
    Genome child = new Genome(this.inputs, this.outputs, true);
    child.connections = new List<Connection>();
    child.nodes = new List<Node>();
    child.layers = this.layers;
    child.nextNode = this.nextNode;
    child.biasNode = this.biasNode;

    List<Connection> childConnections = new List<Connection>();
    List<bool> isEnabled = new List<bool>();

    // combine connections from both parents
    for (int i = 0; i < this.connections.Count; i++)
    {
      bool setEnabled = true;

      // find a matching connection in parent2 by innovation number
      int parent2Connection = MatchingConnection(parent2, this.connections[i].innovationNumber);

      // if a connection is found
      if (parent2Connection != -1)
      {
        // if either connection is disabled, possibly disable in child
        if (!this.connections[i].enabled || !parent2.connections[parent2Connection].enabled)
        {
          if (Random.Range(0f, 1f) < 0.75f)
          {
            setEnabled = false;
          }
        }

        // randomly inherit the connection from one of the parents
        if (Random.Range(0f, 1f) < 0.5f)
        {
          childConnections.Add(this.connections[i]);
        }
        else
        {
          childConnections.Add(parent2.connections[parent2Connection]);
        }
      }
      else
      {
          // if no matching connection, inherit from this parent
          childConnections.Add(connections[i]);
          setEnabled = this.connections[i].enabled;
      }
      isEnabled.Add(setEnabled);
    }

    // clone all nodes from this genome to the child
    foreach (var node in this.nodes)
    {
      child.nodes.Add(node.Clone());
    }

    // add clones connections to the child
    for (int i = 0; i < childConnections.Count; i++)
    {
      Connection clonedConnection = childConnections[i].Clone(
        child.GetNode(childConnections[i].fromNode.id),
        child.GetNode(childConnections[i].toNode.id)
      );
      child.connections.Add(clonedConnection);
      child.connections[i].enabled = isEnabled[i];
    }

    child.ConnectNodes();

    return child;
  }

  // search for a connection in another genome (parent2) that matches a given innovation number
  private int MatchingConnection(Genome parent2, int innovationNumber)
  {
    for (int i = 0; i < parent2.connections.Count; i++)
    {
      if (parent2.connections[i].innovationNumber == innovationNumber)
      {
        return i;
      }
    }

    return -1; // no connections mate
  }

  // no explanation needed
  public Genome Clone()
  {
    Genome clone = new Genome(this.inputs, this.outputs, true);

    foreach (var node in this.nodes)
    {
      clone.nodes.Add(node.Clone());
    }

    // Clone connections with proper node references
    foreach (var connection in this.connections)
    {
      Connection clonedConnection = connection.Clone(
        clone.GetNode(connection.fromNode.id),
        clone.GetNode(connection.toNode.id)
      );
      clone.connections.Add(clonedConnection);
    }

    clone.layers = this.layers;
    clone.nextNode = this.nextNode;
    clone.biasNode = this.biasNode;
    clone.ConnectNodes();

    return clone;
  }
}
