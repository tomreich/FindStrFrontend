/*
 * Copyright (c) 2019 Tom Reich
 * 
 * Licensed under the Microsoft Public License (MS-PL) (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *  https://msdn.microsoft.com/en-us/library/ff649456.aspx
 *  or
 *  https://opensource.org/licenses/MS-PL
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace FindStrFrontend
{
    public class ResultLine
    {
        public ResultLine(string line)
        {
            string[] split = line.Split(':');

            if(split.Length > 2)
            {
                int i = 0;
                if (line.Length > 1 && line[1] == ':') { File = string.Join(":", split[i], split[i + 1]); i++; } else { File = split[i]; } i++;
                Line = split[i++];
                Contents = split[i++];
                while (i < split.Length)
                    Contents += ":" + split[i++];
            }
            else // Something Weird...
            {
                Contents = line;
            }
        }
        public string File { get; set; }
        public string Line { get; set; }
        public string Contents { get; set; }
        public string CSVLine { get { return $"\"{File}\",\"{Line}\",\"{Contents}\""; } }
    }
}
