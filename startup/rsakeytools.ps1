<#
  .SYNPOSIS
    Convert a PrivateKey from the certificate store into a PKCS8 formatted file.
    
  .LINK
    Found C# version here https://gist.github.com/chenrui1988/6b104a010172786dbcbc0aafc466d291/
    
  .NOTES
    Michael West
#> 

class RSAKeyUtils
{
	static [byte[]] PrivateKeyToPKCS8([System.Security.Cryptography.RSAParameters]$privateKey)
	{
		[AsnType]$n = [RSAKeyUtils]::CreateIntegerPos($privateKey.Modulus)
		[AsnType]$e = [RSAKeyUtils]::CreateIntegerPos($privateKey.Exponent)
		[AsnType]$d = [RSAKeyUtils]::CreateIntegerPos($privateKey.D)
		[AsnType]$p = [RSAKeyUtils]::CreateIntegerPos($privateKey.P)
		[AsnType]$q = [RSAKeyUtils]::CreateIntegerPos($privateKey.Q)
		[AsnType]$dp = [RSAKeyUtils]::CreateIntegerPos($privateKey.DP)
		[AsnType]$dq = [RSAKeyUtils]::CreateIntegerPos($privateKey.DQ)
		[AsnType]$iq = [RSAKeyUtils]::CreateIntegerPos($privateKey.InverseQ)
		[AsnType]$version = [RSAKeyUtils]::CreateInteger(@(0))
		[AsnType]$key = [RSAKeyUtils]::CreateOctetString([RSAKeyUtils]::CreateSequence(@($version,$n,$e,$d,$p,$q,$dp,$dq,$iq)))
		[AsnType]$algorithmID = [RSAKeyUtils]::CreateSequence(@([RSAKeyUtils]::CreateOid("1.2.840.113549.1.1.1"),[RSAKeyUtils]::CreateNull()))
		[AsnType]$privateKeyInfo = [RSAKeyUtils]::CreateSequence(@($version,$algorithmID,$key))
		return (New-Object -TypeName AsnMessage -ArgumentList $privateKeyInfo.GetBytes(),"PKCS#8").GetBytes()
	}
	static [byte[]] PrivateKeyToPKCS8([byte[]]$privkey)
	{
		[System.Security.Cryptography.RSAParameters]$RSAParam = [RSAKeyUtils]::DecodeRSAPrivateKeyToRSAParam($privkey)        
		[AsnType]$n = [RSAKeyUtils]::CreateIntegerPos($RSAParam.Modulus)
		[AsnType]$e = [RSAKeyUtils]::CreateIntegerPos($RSAParam.Exponent)
		[AsnType]$d = [RSAKeyUtils]::CreateIntegerPos($RSAParam.D)
		[AsnType]$p = [RSAKeyUtils]::CreateIntegerPos($RSAParam.P)
		[AsnType]$q = [RSAKeyUtils]::CreateIntegerPos($RSAParam.Q)
		[AsnType]$dp = [RSAKeyUtils]::CreateIntegerPos($RSAParam.DP)
		[AsnType]$dq = [RSAKeyUtils]::CreateIntegerPos($RSAParam.DQ)
		[AsnType]$iq = [RSAKeyUtils]::CreateIntegerPos($RSAParam.InverseQ)
		[AsnType]$version = [RSAKeyUtils]::CreateInteger(@(0))
		[AsnType]$key = [RSAKeyUtils]::CreateOctetString([RSAKeyUtils]::CreateSequence(@($version,$n,$e,$d,$p,$q,$dp,$dq,$iq)))
		[AsnType]$algorithmID = [RSAKeyUtils]::CreateSequence(@([RSAKeyUtils]::CreateOid("1.2.840.113549.1.1.1"),[RSAKeyUtils]::CreateNull()))
		[AsnType]$privateKeyInfo = [RSAKeyUtils]::CreateSequence(@($version,$algorithmID,$key))
		return (New-Object -TypeName AsnMessage -ArgumentList $privateKeyInfo.GetBytes(),"PKCS#8").GetBytes()
	}
	static [System.Security.Cryptography.RSAParameters] DecodeRSAPrivateKeyToRSAParam([byte[]]$privkey)
	{
		[System.Security.Cryptography.RSAParameters]$RSAparams = (New-Object -TypeName RSAParameters)
		[byte[]]$MODULUS = [byte[]]
		[byte[]]$E = [byte[]]
		[byte[]]$D = [byte[]]
		[byte[]]$P = [byte[]]
		[byte[]]$Q = [byte[]]
		[byte[]]$DP = [byte[]]
		[byte[]]$DQ = [byte[]]
		[byte[]]$IQ = [byte[]]
		[System.IO.MemoryStream]$mem = (New-Object -TypeName MemoryStream -ArgumentList $privkey)
		[System.IO.BinaryReader]$binr = (New-Object -TypeName BinaryReader -ArgumentList $mem)
		[byte]$bt = 0
		[uint16]$twobytes = 0
		[int]$elems = 0
		try
		{
			$twobytes = $binr.ReadUInt16()
			if ($twobytes -eq 0x8130)
			{
				$binr.ReadByte()
            }
			elseif ($twobytes -eq 0x8230)
			{
				$binr.ReadInt16()
            }
			else
			{
				return (New-Object -TypeName RSAParameters)
            }
			$twobytes = $binr.ReadUInt16()
			if ($twobytes -ne 0x0102)
			{
				return (New-Object -TypeName RSAParameters)
            }
			$bt = $binr.ReadByte()
			if ($bt -ne 0x00)
			{
				return (New-Object -TypeName RSAParameters)
            }
			$elems = [RSAKeyUtils]::GetIntegerSize($binr)
			$MODULUS = $binr.ReadBytes($elems)
			$elems = [RSAKeyUtils]::GetIntegerSize($binr)
			$E = $binr.ReadBytes($elems)
			$elems = [RSAKeyUtils]::GetIntegerSize($binr)
			$D = $binr.ReadBytes($elems)
			$elems = [RSAKeyUtils]::GetIntegerSize($binr)
			$P = $binr.ReadBytes($elems)
			$elems = [RSAKeyUtils]::GetIntegerSize($binr)
			$Q = $binr.ReadBytes($elems)
			$elems = [RSAKeyUtils]::GetIntegerSize($binr)
			$DP = $binr.ReadBytes($elems)
			$elems = [RSAKeyUtils]::GetIntegerSize($binr)
			$DQ = $binr.ReadBytes($elems)
			$elems = [RSAKeyUtils]::GetIntegerSize($binr)
			$IQ = $binr.ReadBytes($elems)
			$RSAparams.Modulus = $MODULUS
			$RSAparams.Exponent = $E
			$RSAparams.D = $D
			$RSAparams.P = $P
			$RSAparams.Q = $Q
			$RSAparams.DP = $DP
			$RSAparams.DQ = $DQ
			$RSAparams.InverseQ = $IQ
			return $RSAparams
		}
		catch [Exception]
		{
			return $RSAparams
		}
		finally
		{
			$binr.Close()
		}
	}
	static [int] GetIntegerSize([System.IO.BinaryReader]$binr)
	{
		[byte]$bt = 0
		[byte]$lowbyte = 0x00
		[byte]$highbyte = 0x00
		[int]$count = 0
		$bt = $binr.ReadByte()
		if ($bt -ne 0x02)
		{
			return 0
        }
		$bt = $binr.ReadByte()
		if ($bt -eq 0x81)
		{
			$count = $binr.ReadByte()
        }
		elseif ($bt -eq 0x82)
		{
			$highbyte = $binr.ReadByte()
			$lowbyte = $binr.ReadByte()
			[byte[]]$modint = [byte[]]::CreateInstance([byte], 4)
            $modint[0] = $lowbyte
            $modint[1] = $highbyte
            $modint[2] = 0x00
            $modint[3] = 0x00
			$count = [BitConverter]::ToInt32($modint,0)
		}
		else
		{
			$count = $bt
		}
		while ($binr.ReadByte() -eq 0x00)
		{
			$count = 1
		}
		$binr.BaseStream.Seek(-1,[System.IO.SeekOrigin]::Current)
		return $count
	}
	static [AsnType] CreateOctetString([AsnType]$value)
	{
		if ([RSAKeyUtils]::IsEmpty($value))
		{
			return (New-Object -TypeName AsnType -ArgumentList ([byte]0x04,[byte[]]@(0x00)))
		}
		return (New-Object -TypeName AsnType -ArgumentList ([byte]0x04,[byte[]]@($value.GetBytes())))
	}
	static [bool] IsEmpty([byte[]]$octets)
	{
		if ($null -eq $octets -or 0 -eq $octets.Length)
		{
			return $true
		}
		return $false
	}
	static [bool] IsEmpty([String]$s)
	{
		if ($null -eq $s -or 0 -eq $s.Length)
		{
			return $true
		}
		return $false
	}
	static [bool] IsEmpty([String[]]$strings)
	{
		if ($null -eq $strings -or 0 -eq $strings.Length)
		{
			return $true
        }
		return $false
	}
	static [bool] IsEmpty([AsnType]$value)
	{
		if ($null -eq $value)
		{
			return $true
		}
		return $false
	}
	static [bool] IsEmpty([AsnType[]]$values)
	{
		if ($null -eq $values -or 0 -eq $values.Length)
		{
			return $true
        }
		return $false
	}
	static [bool] IsEmpty([byte[][]]$arrays)
	{
		if ($null -eq $arrays -or 0 -eq $arrays.Length)
		{
			return $true
        }
		return $false
	}
	static [AsnType] CreateInteger([byte[]]$value)
	{
		if ([RSAKeyUtils]::IsEmpty($value))
		{
            $zero = [byte[]]::CreateInstance([byte], 1)
            $zero[0] = 0
			return [RSAKeyUtils]::CreateInteger($zero)
		}
		return (New-Object -TypeName AsnType -ArgumentList ([byte]0x02,[byte[]]$value))
	}
	static [AsnType] CreateNull()
	{
		return (New-Object -TypeName AsnType -ArgumentList ([byte]0x05,[byte[]]@(0x00)))
	}
	static [byte[]] Duplicate([byte[]]$b)
	{
		if ([RSAKeyUtils]::IsEmpty($b))
		{
            $empty = [byte[]]::CreateInstance([byte], 0)
			return $empty
		}
		[byte[]]$d = [byte[]]::CreateInstance([byte], $b.Length)
		[Array]::Copy($b,$d,$b.Length)
		return $d
	}
	static [AsnType] CreateIntegerPos([byte[]]$value)
	{
		[byte[]]$i = $null
        $d = [RSAKeyUtils]::Duplicate($value)
		if ([RSAKeyUtils]::IsEmpty($d))
		{
            $zero = [byte[]]::CreateInstance([byte], 1)
            $zero[0] = 0
			$d = $zero
		}
		if ($d.Length -gt 0 -and $d[0] -gt 0x7F)
		{
			$i = [byte[]]::CreateInstance([byte], $d.Length + 1)
			$i[0] = 0x00
			[Array]::Copy($d,0,$i,1,$value.Length)
		}
		else
		{
			$i = $d
		}
		return [RSAKeyUtils]::CreateInteger($i)
	}
	static [byte[]] Concatenate([AsnType[]]$values)
	{
		if ([RSAKeyUtils]::IsEmpty($values))
		{
			return [byte[]]::CreateInstance([byte], 0)
        }
		[int]$length = 0
		foreach ($t in $values)
		{
			if ($null -ne $t)
			{
				$length += $t.GetBytes().Length
			}
		}
		[byte[]]$cated = [byte[]]::CreateInstance([byte], $length)
		[int]$current = 0
		foreach ($t in $values)
		{
			if ($null -ne $t)
			{
				[byte[]]$b = $t.GetBytes()
				[Array]::Copy($b,0,$cated,$current,$b.Length)
				$current += $b.Length
			}
		}
		return $cated
	}
	static [AsnType] CreateSequence([AsnType[]]$values)
	{
		if ([RSAKeyUtils]::IsEmpty($values))
		{
			throw (New-Object -TypeName ArgumentException -ArgumentList "A sequence requires at least one value.")
		}
        [byte[]]$octets = [byte[]][RSAKeyUtils]::Concatenate($values)
		return (New-Object -TypeName AsnType -ArgumentList ([byte](0x10 -bor 0x20)), ([byte[]]@($octets)))
	}
	static [AsnType] CreateOid([String]$value)
	{
		if ([RSAKeyUtils]::IsEmpty($value))
		{
			return $null
        }
		[String[]]$tokens = $value.Split(@(' ','.'))
		if ([RSAKeyUtils]::IsEmpty($tokens))
		{
			return $null
        }
		[UInt64]$a = 0
		[System.Collections.Generic.List[UInt64]]$arcs = (New-Object -TypeName System.Collections.Generic.List[UInt64])
		foreach ($t in $tokens)
		{
			if ($t.Length -eq 0)
			{
				break
			}
			try
			{
				$a = [Convert]::ToUInt64($t,[CultureInfo]::InvariantCulture)
			}
			catch [FormatException]
			{
				break
			}
			catch [OverflowException]
			{
				break
			}
			$arcs.Add($a) > $null
		}
		if (0 -eq $arcs.Count)
		{
			return $null
        }
		[System.Collections.Generic.List[byte]]$octets = (New-Object -TypeName System.Collections.Generic.List[byte])
		if ($arcs.Count -ge 1)
		{
			$a = $arcs[0] * 40
		}
		if ($arcs.Count -ge 2)
		{
			$a += $arcs[1]
		}
		$octets.Add([byte]($a))
		for([int]$i = 2; $i -lt $arcs.Count; $i++)
		{
			[System.Collections.Generic.List[byte]]$temp = (New-Object -TypeName System.Collections.Generic.List[byte])
			[UInt64]$arc = $arcs[$i]
			do {
                $temp.Add(([byte]0x80 -bor ($arc -band 0x7F))) > $null
                $arc = $arc -shr 7
            } while (0 -ne $arc)
			[byte[]]$t = $temp.ToArray()
			$t[0] = [byte](0x7F -band $t[0])
			[Array]::Reverse($t)
			foreach ($b in $t)
			{
				$octets.Add($b) > $null
			}
		}
		return [RSAKeyUtils]::CreateOid($octets.ToArray())
	}
	static [AsnType] CreateOid([byte[]]$value)
	{
		if ([RSAKeyUtils]::IsEmpty($value))
		{
			return $null
		}
		return (New-Object -TypeName AsnType -ArgumentList 0x06,$value)
	}
}

class AsnMessage
{
	[byte[]] hidden $Octets
	[String] hidden $m_format
	[int] $Length
	AsnMessage ([byte[]]$octets,[String]$format)
	{
		$this.Octets = $octets
		$this.m_format = $format
	}
	[byte[]] GetBytes()
	{
		if ($null -eq $this.Octets)
		{
			return [byte[]]::CreateInstance([byte], 0)
		}
		return $this.Octets
	}
	[String] GetFormat()
	{
		return $this.m_format
	}
}
class AsnType
{
	AsnType ([byte]$tag,[byte[]]$octets)
	{
		$this.Tag = @($tag)
		$this.Octets = $octets
        $this.Length = [byte[]]::CreateInstance([byte], 0)
	}
	[byte[]] $Tag
	[byte[]] $Length
	[byte[]] $Octets
	[byte[]] GetBytes()
	{
        $this.SetLength()
		if (0x05 -eq $this.Tag[0])
		{
			return $this.Concatenate([byte[][]]@($this.Tag,$this.Octets))
		}
        $val = [byte[][]]@($this.Tag,$this.Length,$this.Octets)
		return $this.Concatenate([byte[][]]@($this.Tag,$this.Length,$this.Octets))
	}
	[void] SetLength()
	{
		if ($null -eq $this.Octets)
		{
            $zero = [byte[]]::CreateInstance([byte], 1)
            $zero[0] = 0
			$this.Length = $zero
			return
		}
		if (0x05 -eq $this.Tag[0])
		{
            $empty = [byte[]]::CreateInstance([byte], 0)
			$this.Length = $empty
			return
		}
		[byte[]]$len = $null
		if ($this.Octets.Length -lt 0x80)
		{
			$len= [byte[]]::CreateInstance([byte], 1)
			$len[0] = [byte]$this.Octets.Length
		}
		elseif ($this.Octets.Length -le 0xFF)
		{
			$len = [byte[]]::CreateInstance([byte], 2)
			$len[0] = 0x81
			$len[1] = [byte](($this.Octets.Length -band 0xFF))
		}
		elseif ($this.Octets.Length -le 0xFFFF)
		{
			$len = [byte[]]::CreateInstance([byte], 3)
			$len[0] = 0x82
			$len[1] = [byte](($this.Octets.Length -band 0xFF00) -shr 8)
			$len[2] = [byte](($this.Octets.Length -band 0xFF))
		}
		elseif ($this.Octets.Length -le 0xFFFFFF)
		{
			$len = [byte[]]::CreateInstance([byte], 4)
			$len[0] = 0x83
			$len[1] = [byte](($this.Octets.Length -band 0xFF0000) -shr 16)
			$len[2] = [byte](($this.Octets.Length -band 0xFF00) -shr 8)
			$len[3] = [byte](($this.Octets.Length -band 0xFF))
		}
		else
		{
			$len = [byte[]]::CreateInstance([byte], 5)
			$len[0] = 0x84
			$len[1] = [byte](($this.Octets.Length -band 0xFF000000) -shr 24)
			$len[2] = [byte](($this.Octets.Length -band 0xFF0000) -shr 16)
			$len[3] = [byte](($this.Octets.Length -band 0xFF00) -shr 8)
			$len[4] = [byte](($this.Octets.Length -band 0xFF))
		}
		$this.Length = $len
	}
	[byte[]] Concatenate([byte[][]]$values)
	{
		if ([RSAKeyUtils]::IsEmpty($values))
		{
			return [byte[]]
        }
		[int]$len = 0
		foreach ($b in $values)
		{
			if ($null -ne $b)
			{
				$len += $b.Length
            }
		}
		[byte[]]$cated = [byte[]]::CreateInstance([byte], $len)
		[int]$current = 0
		foreach ($b in $values)
		{
			if ($null -ne $b)
			{
				[Array]::Copy($b,0,$cated,$current,$b.Length)
				$current += $b.Length
			}
		}
		return $cated
	}
}