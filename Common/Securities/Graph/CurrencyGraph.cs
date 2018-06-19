﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Graph
{
    /// <summary>
    /// Currency Graph, holds currency codes such as "USD" and pairs such as"USDEUR"
    /// </summary>
    public class CurrencyGraph
    {
        public IReadOnlyDictionary<string, CurrencyVertex> Vertices => _vertices;
        public IReadOnlyList<CurrencyEdge> Edges => _edges;

        private Dictionary<string, CurrencyVertex> _vertices;
        private List<CurrencyEdge> _edges;

        private bool _locked = false;

        public CurrencyGraph()
        {
            _vertices = new Dictionary<string, CurrencyVertex>();
            _edges = new List<CurrencyEdge>();
        }

        public CurrencyVertex AddVertex(string Code)
        {
            if (_locked)
                throw new ArgumentException("The graph has been locked, cannot modify the graph anymore!");

            // Add new, if it doesn't already exist
            if (!_vertices.ContainsKey(Code))
                _vertices[Code] = new CurrencyVertex(Code);

            return _vertices[Code];
        }
        
        public CurrencyEdge AddEdge(string CurrencyPair, SecurityType Type)
        {
            if (_locked)
                throw new ArgumentException("The graph has been locked, cannot modify the graph anymore!");

            string baseCode = null;
            string quoteCode = null;

            Forex.Forex.DecomposeCurrencyPair(CurrencyPair, out baseCode, out quoteCode);

            return AddEdge(baseCode, quoteCode, Type);
        }

        public CurrencyEdge AddEdge(string BaseCode, string QuoteCode, SecurityType Type)
        {
            if (_locked)
                throw new ArgumentException("The graph has been locked, cannot modify the graph anymore!");

            // Search if existing edge already exists
            foreach (CurrencyEdge strEdge in Edges)
            {
                CurrencyEdge.Match match = strEdge.CompareTo(BaseCode, QuoteCode);

                if (match == CurrencyEdge.Match.ExactMatch)
                    return strEdge;

                if (match == CurrencyEdge.Match.InverseMatch)
                {
                    strEdge.MakeBidirectional();
                    return strEdge;
                }
            }
            
            // No existing edge, add new
            var vertexA = AddVertex(BaseCode);
            var vertexB = AddVertex(QuoteCode);

            var edge = new CurrencyEdge(vertexA, vertexB, Type);
            
            vertexA.AddEdge(edge);
            vertexB.AddEdge(edge);

            _edges.Add(edge);

            return edge;
        }
        
        /// <summary>
        /// Uses BFS to search through this graph
        /// </summary>
        /// <param name="StartCode">Start of currency code</param>
        /// <param name="EndCode">End of currency code</param>
        /// <returns></returns>
        public CurrencyPath FindShortestPath(string StartCode, string EndCode)
        {
            CurrencyVertex startVertex = null;

            try {
                startVertex = _vertices[StartCode];
            }
            catch {
                throw new ArgumentException($"No path found, graph does not contain StartCode: {StartCode}");
            }
            
            HashSet<string> processedNodes = new HashSet<string>();
            Queue<CurrencyPath> pathsToExtend = new Queue<CurrencyPath>();

            pathsToExtend.Enqueue(new CurrencyPath(startVertex));
            
            while (pathsToExtend.Count > 0)
            {
                CurrencyPath path = pathsToExtend.Dequeue();

                if(path.EndVertex.Code == EndCode)
                    return path;

                processedNodes.Add(path.EndVertex.Code);

                // grow paths
                foreach (CurrencyEdge edge in path.EndVertex.Edges)
                {
                    //find other endpoint of edge
                    CurrencyVertex vertex = edge.Other(path.EndVertex);
                    
                    // check if this endpoint has been NOT visited already then if, add the extended path to queue
                    if (!processedNodes.Contains(vertex.Code))
                        pathsToExtend.Enqueue(path.Extend(edge));
                }
            }

            throw new ArgumentException($"No path found, graph does not contain EndCode: {EndCode}, or no possible path");
        }
        
        /// <summary>
        /// Locks the Graph from editing, makes it read only.
        /// </summary>
        public void Lock()
        {
            // also make vertices read-only
            foreach(var vertex in _vertices.Values)
                vertex.Lock();
            
            _locked = true;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Vertices:");
            foreach (var vertex in Vertices.Values)
                builder.AppendLine(vertex.ToString());
            
            builder.AppendLine("");
            builder.AppendLine("Edges:");
            
            foreach(var edge in Edges)
                builder.AppendLine(edge.ToString());
            

            return builder.ToString();
        }

        /// <summary>
        /// Make a complete copy of the graph. The copy will be also unlocked, so it can be modified freely.
        /// </summary>
        /// <returns>Copy of this instance</returns>
        public CurrencyGraph Copy()
        {
            CurrencyGraph copy = new CurrencyGraph();
            
            foreach(var code in _vertices.Keys)
                copy.AddVertex(code);

            foreach (var edge in _edges)
                copy.AddEdge(edge.PairSymbol, edge.Type);

            return copy;
        }
    }
}