using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace Playkids.Match3
{
    public class Board
    {
        [ShowInInspector]
        public BoardConfig Config { private set; get; }

        [ShowInInspector]
        private readonly Tile[][] tiles;

        [ShowInInspector]
        private PieceType[,] debugPieceBoard
        {
            get
            {
                PieceType[,] pieceBoard = new PieceType[Config.BoardSize.x, Config.BoardSize.y];

                for (int x = 0; x < tiles.Length; x++)
                {
                    for (int y = 0; y < tiles[x].Length; y++)
                    {
                        pieceBoard[x, y] = tiles[x][y].Piece?.Config.Type ?? PieceType.Empty;
                    }
                }
                
                return pieceBoard;
            }
        }

        private readonly List<PieceConfig> basicPieces;
        private readonly List<PiecePatternConfig> mergedSortedPiecePatterns = new List<PiecePatternConfig>();

        public Board(BoardConfig config)
        {
            Config = config;
            tiles = config.Board;

            basicPieces = PiecesDB.Instance.FindAll(item => item.Category == PieceCategory.Basic);
            CachePatterns();

            for (int x = 0; x < tiles.Length; x++)
            {
                for (int y = 0; y < tiles[x].Length; y++)
                {
                    tiles[x][y].Initialize(this);

                    if (!tiles[x][y].HasPiece)
                    {
                        Piece newPiece = GenerateRandomBasicPiece();
                        MovePieceTo(newPiece, tiles[x][y]);
                    }
                }
            }
        }

        public Tile GetTileAt(Vector2Int position)
        {
            if (position.x >= 0 && position.x < tiles.Length)
            {
                if (position.y >= 0 && position.y < tiles[position.x].Length)
                {
                    return tiles[position.x][position.y];
                }
            }

            return null;
        }

        public bool MovePieceTo(Piece piece, Tile tile)
        {
            if (!piece.IsPlaced && !tile.HasPiece && tile.CanPutPiece)
            {
                piece.Tile = tile;
                tile.Piece = piece;
                return true;
            }

            return false;
        }

        public PatternSearchResult FindPatterns()
        {
            PatternSearchResult searchResult = new PatternSearchResult(); //Stores occurrences of patterns
            HashSet<Tile> allPatternsTiles = new HashSet<Tile>(); //Stores all the tiles found in the patterns, this list is used to check the intersection between the patterns.
            
            //For each pattern (the patterns are ordered in descending order to give more priority to the patterns that have more points
            foreach (PiecePatternConfig matchPattern in mergedSortedPiecePatterns) 
            {
                for (int x = 0; x < Config.BoardSize.x; x++)
                {
                    for (int y = 0; y < Config.BoardSize.y; y++)
                    {
                        foreach (PieceConfig basicPiece in basicPieces)
                        {
                            //Search from this tile on the board if there is any pattern that contains the same type of piece
                            List<Tile> tilesFoundInPattern = GetTilesWithSamePiecesFollowingPattern(new Vector2Int(x, y),
                                matchPattern, basicPiece.Type);

                            if (tilesFoundInPattern == null)
                            {
                                continue;
                            }
                            
                            bool tileHasAlreadyBeenFoundInAnyPattern = false; //Flag that prevents a pattern from being found in another pattern
                                
                            //Checks whether the elements in tilesFoundInPattern list intersect with the elements in allPatternsTiles list
                            foreach (Tile tileFoundInPattern in tilesFoundInPattern)
                            {
                                if (allPatternsTiles.Contains(tileFoundInPattern))
                                {
                                    tileHasAlreadyBeenFoundInAnyPattern = true;
                                    break;
                                }
                            }

                            //Only added to the list of found patterns if it does not intersect with any tiles previously found
                            if (!tileHasAlreadyBeenFoundInAnyPattern)
                            {
                                PatternFound newPatternFound = new PatternFound(matchPattern, tilesFoundInPattern);

                                if (matchPattern.IsHint)
                                {
                                    searchResult.Hints.Add(newPatternFound);
                                }
                                else
                                {
                                    searchResult.Matchs.Add(newPatternFound);
                                }
                                
                                allPatternsTiles.AddRange(tilesFoundInPattern);
                            }
                        }
                    }
                }
            }
            
            return searchResult;
        }

        /// <summary>
        /// Search from tile position on the board if there is any pattern that contains the same type of piece
        /// </summary>
        /// <param name="startBoardIndex"></param>
        /// <param name="allPatternCoords"></param>
        /// <param name="pieceType"></param>
        /// <returns>Returns the list of tiles containing this pattern, if not found a pattern returns null</returns>
        public List<Tile> GetTilesWithSamePiecesFollowingPattern(Vector2Int startBoardIndex, PiecePatternConfig patternConfig, PieceType pieceType)
        {
            List<Tile> tiles = new List<Tile>();
            List<Vector2Int> allPatternCoords = patternConfig.PatternCoords;
            foreach (Vector2Int patternCoords in allPatternCoords)
            {
                Tile tile = GetTileAt(startBoardIndex + patternCoords);
                if (tile == null || !tile.HasPiece || tile.Piece.Config.Type != pieceType)
                {
                    return null;
                }
                tiles.Add(tile);
            }
            
            return tiles;
        }

        private void CachePatterns()
        {
            List<PiecePatternConfig> sortedMatchPatterns = new List<PiecePatternConfig>(PiecePatternDB.Instance.MatchPatterns);
            sortedMatchPatterns.Sort((a, b) => b.Score.CompareTo(a.Score)); //Sort desc
            
            List<PiecePatternConfig> sortedHintPatterns = new List<PiecePatternConfig>(PiecePatternDB.Instance.HintPatterns);
            sortedHintPatterns.Sort((a, b) => b.Score.CompareTo(a.Score)); //Sort desc
            
            mergedSortedPiecePatterns.AddRange(sortedMatchPatterns); //Add matches first to take priority
            mergedSortedPiecePatterns.AddRange(sortedHintPatterns);
        }
        
        private Piece GenerateRandomBasicPiece()
        {
            return new Piece(basicPieces[UnityEngine.Random.Range(0, basicPieces.Count)]);
        }
    }
}
