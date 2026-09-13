#!/usr/bin/env perl
# Checks that a commit message is written in English (see CLAUDE.md).
# Cyrillic is allowed only inside `code`, ```fences```, "quotes" and «quotes».
#
# Usage: check-commit-message.pl <message-file> [label]
# Used by .githooks/commit-msg locally and by .github/workflows/commit-messages.yml in PRs.
use strict;
use warnings;
use utf8;
use open qw(:std :encoding(UTF-8));

my ($file, $label) = @ARGV;
die "usage: $0 <message-file> [label]\n" unless defined $file;
$label //= 'commit message';

open my $fh, '<', $file or die "cannot read $file: $!\n";
my $text = do { local $/; <$fh> };
close $fh;

# Git may leave its (possibly localized) comments and the `commit -v` diff in the file.
$text =~ s/^# -+ >8 -+\n.*//ms;
$text =~ s/^#.*$//mg;

# Blank out allowed spans, keeping newlines so line numbers still match.
my $masked = $text;
my $blank = sub { my ($s) = @_; $s =~ s/[^\n]/ /g; $s };
$masked =~ s/(```.*?```)/$blank->($1)/gse;
$masked =~ s/(`[^`\n]*`)/$blank->($1)/ge;
$masked =~ s/("[^"\n]*")/$blank->($1)/ge;
$masked =~ s/(«[^»\n]*»)/$blank->($1)/ge;
$masked =~ s/(“[^”\n]*”)/$blank->($1)/ge;

my @original = split /\n/, $text, -1;
my @checked  = split /\n/, $masked, -1;
my @bad = grep { $checked[$_] =~ /\p{Cyrillic}/ } 0 .. $#checked;
exit 0 unless @bad;

print STDERR "$label: must be written in English.\n";
printf STDERR "  line %d: %s\n", $_ + 1, $original[$_] for @bad;
print STDERR "Russian is allowed only as a literal or quote: `...`, \"...\" or «...».\n";
exit 1;
