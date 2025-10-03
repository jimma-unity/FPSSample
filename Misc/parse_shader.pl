#!/usr/bin/perl
use strict;
use warnings;

# Usage check
my $logfile = $ARGV[0] or die "Usage: $0 <Player.log>\n";
open my $fh, '<', $logfile or die "Cannot open $logfile: $!";

my %variants;
my $lines_found = 0;

while (my $line = <$fh>) {
    chomp $line;

    # Check for our phrase
    if (index($line, 'Uploaded shader variant to the GPU driver:') != -1) {
        $lines_found++;

        # Remove leading phrase
        my $prefix = 'Uploaded shader variant to the GPU driver:';
        $line = substr($line, length($prefix));
        $line =~ s/^\s+//;

        # Split by commas
        my @parts = split(/,/, $line);

        # Shader name
        my $shader_part = shift @parts;
        my $shader = $shader_part;
        if (index($shader, '(') != -1) {
            $shader = substr($shader, 0, index($shader, '('));
        }
        $shader =~ s/^\s+//;
        $shader =~ s/\s+$//;

        # Pass
        my $pass_part = shift @parts;
        $pass_part =~ s/^\s+//;
        $pass_part =~ s/\s+$//;
        my (undef, $pass) = split(/:\s*/, $pass_part, 2);
        $pass =~ s/^\s+//;
        $pass =~ s/\s+$//;

        # Stage
        my $stage_part = shift @parts;
        $stage_part =~ s/^\s+//;
        $stage_part =~ s/\s+$//;
        my (undef, $stage) = split(/:\s*/, $stage_part, 2);
        $stage =~ s/^\s+//;
        $stage =~ s/\s+$//;

        # Keywords
        my $keywords_part = shift @parts;
        $keywords_part =~ s/^\s+//;
        $keywords_part =~ s/\s+$//;
        my ($kw_label, $keywords_str) = split(/\s+/, $keywords_part, 2);
        $keywords_str =~ s/^\s+// if defined $keywords_str;
        $keywords_str =~ s/\s+$// if defined $keywords_str;
        $keywords_str = '' if !defined $keywords_str || $keywords_str eq '<no keywords>';

        my @sorted_keywords = sort grep { $_ ne '' } split(/\s+/, $keywords_str);

        # Unique key includes stage
        my $key = join('|', $shader, $pass, $stage, join(',', @sorted_keywords));
        $variants{$key} = [$shader, $pass, $stage, \@sorted_keywords];
    }
}

close $fh;

# No matches
if ($lines_found == 0) {
    print "// No 'Uploaded shader variant...' lines found in $logfile.\n";
    exit 1;
}

# No variants parsed
if (!%variants) {
    print "// No variants parsed. Check your log file.\n";
    exit 1;
}

# Output LoadRules function
print "private List<ShaderVariantRule> LoadRules()\n";
print "{\n";
print "    var rules = new List<ShaderVariantRule>\n";
print "    {\n";
foreach my $key (sort keys %variants) {
    my ($shader, $pass, $stage, $keywords_ref) = @{$variants{$key}};
    my @keywords = @$keywords_ref;
    print qq{        new("$shader", "$pass", "$stage", new List<string>\n};
    print "            {\n";
    foreach my $k (@keywords) {
        print qq{                "$k",\n};
    }
    print "            }),\n";
}
print "    };\n";
print "    return rules;\n";
print "}\n";
