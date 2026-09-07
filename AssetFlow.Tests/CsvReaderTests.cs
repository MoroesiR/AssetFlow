using System.Linq;
using AssetFlow.Services;
using Xunit;

namespace AssetFlow.Tests
{
    // The reader exists because splitting on commas breaks on the first description
    // containing one, which is most real inventory exports. These are the cases that
    // justify it.
    public class CsvReaderTests
    {
        [Fact]
        public void Splits_a_plain_row()
        {
            var rows = CsvReader.Parse("Name,Serial\nLaptop,ABC-1");

            Assert.Equal(2, rows.Count);
            Assert.Equal(new[] { "Laptop", "ABC-1" }, rows[1]);
        }

        [Fact]
        public void Keeps_a_comma_inside_quotes_in_one_field()
        {
            var rows = CsvReader.Parse("Name,Notes\nChair,\"Ergonomic, adjustable arms\"");

            Assert.Equal(2, rows[1].Count);
            Assert.Equal("Ergonomic, adjustable arms", rows[1][1]);
        }

        [Fact]
        public void Doubled_quotes_become_one_literal_quote()
        {
            var rows = CsvReader.Parse("Name\n\"A \"\"special\"\" case\"");

            Assert.Equal("A \"special\" case", rows[1][0]);
        }

        [Fact]
        public void A_newline_inside_quotes_does_not_start_a_new_row()
        {
            var rows = CsvReader.Parse("Name,Notes\nDesk,\"line one\nline two\"");

            Assert.Equal(2, rows.Count);
            Assert.Contains("\n", rows[1][1]);
        }

        [Fact]
        public void Handles_the_CRLF_that_Excel_writes()
        {
            var rows = CsvReader.Parse("Name,Serial\r\nLaptop,ABC-1\r\n");

            Assert.Equal(2, rows.Count);
            Assert.Equal("ABC-1", rows[1][1]);
        }

        [Fact]
        public void Drops_a_trailing_blank_line()
        {
            var rows = CsvReader.Parse("Name\nLaptop\n\n");

            Assert.Equal(2, rows.Count);
        }

        [Fact]
        public void Keeps_empty_fields_in_place_rather_than_collapsing_them()
        {
            var rows = CsvReader.Parse("A,B,C\n1,,3");

            Assert.Equal(3, rows[1].Count);
            Assert.Equal("", rows[1][1]);
            Assert.Equal("3", rows[1][2]);
        }

        [Fact]
        public void An_empty_file_yields_nothing()
        {
            Assert.Empty(CsvReader.Parse(""));
        }
    }
}
