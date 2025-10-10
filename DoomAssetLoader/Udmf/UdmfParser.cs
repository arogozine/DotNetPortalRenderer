using static System.MemoryExtensions;

namespace DoomAssetLoader.Udmf
{
    internal static partial class UdmfParser
    {
        public static UdmfMapData Parse(ReadOnlySpan<char> textmapContent)
        {
            var mapData = new UdmfMapData();

            SpanSplitEnumerator<char> lines = textmapContent.Split('\n');

            /*
                translation_unit := global_expr_list
                global_expr_list := global_expr global_expr_list
                global_expr := block | assignment_expr
                block := identifier '{' expr_list '}'
                expr_list := assignment_expr expr_list
                assignment_expr := identifier '=' value ';' | nil
                identifier := [A-Za-z_]+[A-Za-z0-9_]*
                value := integer | float | quoted_string | keyword
                integer := [+-]?[1-9]+[0-9]* | 0[0-9]+ | 0x[0-9A-Fa-f]+
                float := [+-]?[0-9]+'.'[0-9]*([eE][+-]?[0-9]+)?
                quoted_string := "([^"\\]*(\\.[^"\\]*)*)"
                keyword := [^{}();"'\n\t ]+
            */

            const string commentBlockStart = "/*";
            const string commentBlockEnd = "*/";

            bool commentBlock = false;
            bool expectKey = true;
            bool expectBlock = false;
            bool expectEndBlock = false;
            bool expectIdentifier = false;
            bool expectEquals = false;
            bool expectValue = false;

            UdmfObject? udmfObject = null;

            string? identifierName = null;

            foreach (Range lineRange in lines)
            {
                ReadOnlySpan<char> line = textmapContent[lineRange];

                if (commentBlock)
                {
                    int commentBlockEndIndex = line.IndexOf(commentBlockEnd);

                    if (commentBlockEndIndex == -1)
                    {
                        continue;
                    }

                    commentBlockEndIndex += commentBlockEnd.Length;
                    line = line[commentBlockEndIndex..];
                }
                else
                {
                    int commentBlockStartIndex = line.IndexOf(commentBlockStart);

                    if (commentBlockStartIndex != -1)
                    {
                        commentBlock = true;
                        line = line[..commentBlockStartIndex];
                    }

                }

                line = TrimAndStripComment(line);

                if (line.Length == 0)
                {
                    continue;
                }

                foreach (Range tokenRange in line.Split(' '))
                {
                    ReadOnlySpan<char> token = line[tokenRange].Trim();

                    if (token.Length == 0) { continue; }

                    if (token.SequenceEqual("namespace")) { break; }

                    if (expectKey)
                    {
                        expectKey = false;
                        expectBlock = true;

                        switch (token)
                        {
                            case "vertex":
                                {
                                    var _udmfObject = new UdmfVertex();
                                    mapData.Vertices.Add(_udmfObject);
                                    udmfObject = _udmfObject;
                                    break;
                                }
                            case "linedef":
                                {
                                    var _udmfObject = new UdmfLinedef();
                                    mapData.Linedefs.Add(_udmfObject);
                                    udmfObject = _udmfObject;
                                    break;
                                }
                            case "sidedef":
                                {
                                    var _udmfObject = new UdmfSidedef();
                                    mapData.Sidedefs.Add(_udmfObject);
                                    udmfObject = _udmfObject;
                                    break;
                                }
                            case "sector":
                                {
                                    var _udmfObject = new UdmfSector();
                                    mapData.Sectors.Add(_udmfObject);
                                    udmfObject = _udmfObject;
                                    break;
                                }
                            case "thing":
                                {
                                    var _udmfObject = new UdmfThing();
                                    mapData.Things.Add(_udmfObject);
                                    udmfObject = _udmfObject;
                                    break;
                                }
                        }

                        continue;
                    }

                    if (expectBlock && token.SequenceEqual("{"))
                    {
                        expectBlock = false;
                        expectEndBlock = true;
                        expectIdentifier = true;

                        continue;
                    }

                    if (expectEndBlock && token.SequenceEqual("}"))
                    {
                        expectEndBlock = false;
                        expectKey = true;
                        expectIdentifier = false;
                        continue;
                    }

                    if (expectIdentifier)
                    {
                        identifierName = new string(token);
                        expectIdentifier = false;
                        expectEquals = true;
                        continue;
                    }

                    if (expectEquals && token.SequenceEqual("="))
                    {
                        expectEquals = false;
                        expectValue = true;
                        continue;
                    }

                    if (expectValue)
                    {
                        if (token[^1] == ';')
                        {
                            token = token[..^1];
                        }

                        if (token.Length > 2 && token[0] == '"' && token[^1] == '"')
                        {
                            token = token[1..^1];
                        }

                        udmfObject.Add(identifierName, new string(token));

                        expectValue = false;
                        expectIdentifier = true;
                    }
                }
            }

            return mapData;
        }


        private static ReadOnlySpan<char> TrimAndStripComment(ReadOnlySpan<char> line)
        {
            int indexOfComment = line.IndexOf("//");

            if (indexOfComment != -1)
            {
                line = line[..indexOfComment];
            }

            return line.Trim();
        }
    }
}
